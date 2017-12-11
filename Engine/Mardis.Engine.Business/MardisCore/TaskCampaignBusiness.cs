using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Mardis.Engine.Converter;
using Mardis.Engine.DataAccess;
using Mardis.Engine.DataAccess.MardisCore;
using Mardis.Engine.DataObject.MardisCommon;
using Mardis.Engine.DataObject.MardisCore;
using Mardis.Engine.DataObject.MardisSecurity;
using Mardis.Engine.Framework;
using Mardis.Engine.Framework.Resources;
using Mardis.Engine.Framework.Resources.PagesConstants;
using Mardis.Engine.Web.ViewModel.Filter;
using Mardis.Engine.Web.ViewModel.TaskViewModels;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml.Schema;
using AutoMapper;
using Mardis.Engine.Business.MardisSecurity;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using StackExchange.Redis;
using Microsoft.Extensions.Caching.Distributed;

namespace Mardis.Engine.Business.MardisCore
{
    public class TaskCampaignBusiness : ABusiness
    {
        #region VARIABLES Y CONSTRUCTORES

        private readonly TaskCampaignDao _taskCampaignDao;
        private readonly QuestionDao _questionDao;
        private readonly QuestionDetailDao _questionDetailDao;
        private readonly StatusTaskBusiness _statusTaskBusiness;
        private readonly SequenceBusiness _sequenceBusiness;
        private readonly CampaignServicesDao _campaignServicesDao;
        private readonly BranchDao _branchDao;
        private readonly AnswerDetailDao _answerDetailDao;
        private readonly AnswerDao _answerDao;
        private readonly BranchImageBusiness _branchImageBusiness;
        private readonly UserDao _userDao;
        private readonly CampaignDao _campaignDao;
        private readonly ServiceDetailTaskBusiness _serviceDetailTaskBusiness;
        private readonly PersonDao _personDao;
        private readonly ProfileDao _profileDao;
        private readonly TypeUserBusiness _typeUserBusiness;
        private readonly ServiceDetailDao _serviceDetailDao;
        private readonly RedisCache _redisCache;
        private readonly ServiceDetailBusiness _serviceDetailBusiness;

        public TaskCampaignBusiness(MardisContext mardisContext, RedisCache distributedCache)
            : base(mardisContext)
        {
            _taskCampaignDao = new TaskCampaignDao(mardisContext);
            _questionDetailDao = new QuestionDetailDao(mardisContext);
            _statusTaskBusiness = new StatusTaskBusiness(mardisContext, distributedCache);
            _sequenceBusiness = new SequenceBusiness(mardisContext);
            _campaignServicesDao = new CampaignServicesDao(mardisContext);
            _branchDao = new BranchDao(mardisContext);
            _answerDao = new AnswerDao(mardisContext);
            _answerDetailDao = new AnswerDetailDao(mardisContext);
            _branchImageBusiness = new BranchImageBusiness(mardisContext);
            _userDao = new UserDao(mardisContext);
            _campaignDao = new CampaignDao(mardisContext);
            _serviceDetailTaskBusiness = new ServiceDetailTaskBusiness(mardisContext);
            _personDao = new PersonDao(mardisContext);
            _profileDao = new ProfileDao(mardisContext);
            _typeUserBusiness = new TypeUserBusiness(mardisContext, distributedCache);
            _serviceDetailDao = new ServiceDetailDao(mardisContext);
            _questionDao = new QuestionDao(mardisContext);
            _redisCache = distributedCache;
            _serviceDetailBusiness = new ServiceDetailBusiness(mardisContext);

            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<Service, MyTaskServicesViewModel>()
                    .ForMember(dest => dest.ServiceDetailCollection, opt => opt.MapFrom(src => src.ServiceDetails.OrderBy(sd => sd.Order)));
                cfg.CreateMap<ServiceDetail, MyTaskServicesDetailViewModel>()
                    .ForMember(dest => dest.QuestionCollection, opt => opt.MapFrom(src => src.Questions.OrderBy(q => q.Order)))
                    .ForMember(dest => dest.Sections, opt => opt.MapFrom(src => src.Sections.OrderBy(s => s.Order)));
                cfg.CreateMap<Question, MyTaskQuestionsViewModel>()
                    .ForMember(dest => dest.HasPhoto, opt => opt.MapFrom(src => src.HasPhoto.IndexOf("S", StringComparison.Ordinal) >= 0))
                    .ForMember(dest => dest.QuestionDetailCollection, opt => opt.MapFrom(src => src.QuestionDetails.OrderBy(qd => qd.Order)))
                    .ForMember(dest => dest.CodeTypePoll, opt => opt.MapFrom(src => src.TypePoll.Code));
                cfg.CreateMap<QuestionDetail, MyTaskQuestionDetailsViewModel>();
            });
        }

        #endregion

        public TaskListViewModel GetPaginatedTasksList(Guid idCampaign, Guid idAccount, List<FilterValue> filters, int pageIndex, int pageSize)
        {
            var watch = new Stopwatch();

            var itemResult = new TaskListViewModel { IdCampaign = idCampaign };

            filters = filters ?? new List<FilterValue>();

            filters = AddHiddenFilter("IdCampaign", idCampaign.ToString(), filters, itemResult.FilterName);

            var tasks = _taskCampaignDao.GetPaginatedTasksList(idAccount, filters, pageIndex, pageSize);
            var countTasks = _taskCampaignDao.GetPaginatedTasksCount(idAccount, filters);

            foreach (var task in tasks)
            {
                var branch = _branchDao.GetOne(task.IdBranch, idAccount);
                var merchant = _userDao.GetUserById(task.IdMerchant);
                watch.Start();
                var status = _statusTaskBusiness.GetStatusTask(task.IdStatusTask);
                watch.Stop();
                var campaign = _campaignDao.GetOne(task.IdCampaign, idAccount);
                var tvm = new TaskListItemViewModel()
                {
                    BranchName = branch.Name,
                    Code = task.Code,
                    Id = task.Id,
                    MerchantName = merchant.Person.Name + " " + merchant.Person.SurName,
                    Route = task.Route,
                    StartDate = task.StartDate,
                    StatusName = status.Name,
                    CampaignId = task.IdCampaign,
                    CampaignName = campaign.Name,
                    BranchCode = branch.ExternalCode
                };

                itemResult.TasksList.Add(tvm);
            }

            Debugger.Log(0, "TaskCapaugnBusiness", $" ms: {watch.ElapsedMilliseconds}");

            return ConfigurePagination(itemResult, pageIndex, pageSize, filters, countTasks);
        }

        public TaskPerCampaignViewModel GetTasksPerCampaign(Guid userId, int pageIndex, int pageSize, List<FilterValue> filters, Guid idAccount)
        {
            filters = filters ?? new List<FilterValue>();

            var itemResult = new TaskPerCampaignViewModel("MyTasks", "Task");
            var user = _userDao.GetUserById(userId);

            if (user.Profile.TypeUser.Name == CTypePerson.PersonMerchant)
            {
                filters = AddHiddenFilter("IdMerchant", userId.ToString(), filters, itemResult.FilterName);
            }

            int max;

            itemResult = GetTasksProperties(pageIndex, pageSize, filters, idAccount, itemResult, out max);

            return ConfigurePagination(itemResult, pageIndex, pageSize, filters, max);
        }

        private TaskPerCampaignViewModel GetTasksProperties(int pageIndex, int pageSize, List<FilterValue> filters, Guid idAccount,
              TaskPerCampaignViewModel itemResult, out int max)
        {
            max = _taskCampaignDao.GetTaskCountByCampaignAndStatus(CTask.StatusImplemented, filters, idAccount);

            var countImplementedTasks = max;
            var countNotImplementedTasks = _taskCampaignDao.GetTaskCountByCampaignAndStatus(CTask.StatusNotImplemented, filters,
                idAccount);

            max = (max > countNotImplementedTasks) ? max : countNotImplementedTasks;

            var countPendingTasks = _taskCampaignDao.GetTaskCountByCampaignAndStatus(CTask.StatusPending, filters, idAccount);

            max = (max > countPendingTasks) ? max : countPendingTasks;

            var countStartedTasks = _taskCampaignDao.GetTaskCountByCampaignAndStatus(CTask.StatusStarted, filters, idAccount);

            max = (max > countStartedTasks) ? max : countStartedTasks;

            itemResult.ImplementedTasksList = GetTaskList(pageIndex, pageSize, filters, idAccount,
                CTask.StatusImplemented);

            itemResult.NotImplementedTasksList = GetTaskList(pageIndex, pageSize, filters, idAccount,
                CTask.StatusNotImplemented);

            itemResult.PendingTasksList = GetTaskList(pageIndex, pageSize, filters, idAccount,
                CTask.StatusPending);

            itemResult.StartedTasksList = GetTaskList(pageIndex, pageSize, filters, idAccount,
                CTask.StatusStarted);

            itemResult.CountImplementedTasks = countImplementedTasks;
            itemResult.CountNotImplementedTasks = countNotImplementedTasks;
            itemResult.CountPendingTasks = countPendingTasks;
            itemResult.CountStartedTasks = countStartedTasks;

            return itemResult;
        }

        private List<MyTaskItemViewModel> GetTaskList(int pageIndex, int pageSize, List<FilterValue> filters, Guid idAccount, string statusTask)
        {
            var tasks = _taskCampaignDao.GetPaginatedTasksByCampaignAndStatus(statusTask, pageIndex, pageSize, filters,
                idAccount);

            return ConvertTask.ConvertTaskToMyTaskViewItemModel(tasks);
        }

        public TaskCampaign GetTaskByIdForRegisterPage(Guid idTask, Guid idAccount)
        {
            return _taskCampaignDao.GetTaskByIdForRegisterPage(idTask, idAccount);
        }

        public List<TaskCampaign> GetAlltasksByCampaignId(Guid idCampaign, Guid idAccount)
        {
            return _taskCampaignDao.GetTaskListByCampaign(idCampaign, idAccount);
        }

        public bool ModifyTask(TaskCampaign task)
        {
            using (var transaccion = Context.Database.BeginTransaction())
            {
                try
                {
                    Context.TaskCampaigns.Add(task);

                    Context.Entry(task).State = EntityState.Modified;

                    Context.SaveChanges();
                    transaccion.Commit();
                }
                catch (Exception)
                {
                    transaccion.Rollback();
                    return false;
                }

            }
            return true;
        }

        public TaskCampaign SaveTaskRegister(string inputTask, Guid idAccount)
        {
            TaskCampaign result;
            var taskModelView = JSonConvertUtil.Deserialize<TaskRegisterModelView>(inputTask);
            var task = new TaskCampaign()
            {
                IdAccount = idAccount,
                IdBranch = taskModelView.IdBranch,
                IdCampaign = taskModelView.IdCampaign,
                IdMerchant = taskModelView.IdMerchant,
                IdStatusTask = taskModelView.IdStatusTask,
                Description = taskModelView.Description,
                StartDate = taskModelView.StartDate,
                Campaign = null,
                Branch = null
            };
            if (taskModelView.IdStatusTask == Guid.Empty)
            {
                var status = _statusTaskBusiness.GeStatusTaskByName(CTask.StatusPending);
                task.IdStatusTask = status.Id;
            }

            using (var transaccion = Context.Database.BeginTransaction())
            {
                try
                {
                    if (string.IsNullOrEmpty(task.Code))
                    {
                        var nextSequence = _sequenceBusiness.NextSequence(CTask.SequenceCode,
                            idAccount);

                        task.Code = nextSequence.ToString();
                    }

                    var stateRegister = EntityState.Added;

                    if (Guid.Empty != task.Id)
                    {
                        stateRegister = EntityState.Modified;
                    }

                    Context.TaskCampaigns.Add(task);

                    Context.Entry(task).State = stateRegister;

                    //graba cambios en COntexto
                    Context.SaveChanges();
                    transaccion.Commit();
                    result = GetTaskByIdForRegisterPage(task.Id, idAccount);
                }
                catch
                {
                    transaccion.Rollback();
                    result = null;
                }
            }

            return result;
        }

        public List<TaskCampaign> GetTaskListByServiceAndBranch(Guid idBranch, Guid idService, Guid idAccount)
        {
            var campaignServicesList = _campaignServicesDao.GetCampaignServicesByService(idService, idAccount);

            Guid[] campaignIds = campaignServicesList.Select(cs => cs.IdCampaign).ToArray();

            return _taskCampaignDao.GetTaskListByCampaignIdsAndBranch(idBranch, campaignIds, idAccount);
        }

        public List<TaskCampaign> GetTasksBranchesByCampaign(Guid idCampaign, Guid idAccount)
        {
            var taskList = _taskCampaignDao.GetAlltasksByCampaignId(idCampaign, idAccount);

            foreach (var task in taskList)
            {
                task.Branch = _branchDao.GetOne(task.IdBranch, idAccount);
                task.StatusTask = _statusTaskBusiness.GetStatusTask(task.IdStatusTask);
            }

            return taskList;
        }


        //Metodo devuelve las tareas y campañas
        public List<MyTaskViewModel> GetSectionsPollLista(Guid idAccount)
        {

            var taskWithPoll = _taskCampaignDao.GetForProfileLista(idAccount);
            if (taskWithPoll == null)
            {
                return null;
            }
            var myWatch = new Stopwatch();
            myWatch.Start();
            myWatch.Stop();
            return taskWithPoll;

        }





        /// <summary>
        /// Este método Obtiene la estructura de la encuesta sin Preguntas ni subsecciones
        /// </summary>
        /// <param name="idTask">Identificador de Tarea</param>
        /// <param name="idAccount">Identificador de cuenta</param>
        /// <returns></returns>
        public MyTaskViewModel GetSectionsPoll(Guid idTask, Guid idAccount)
        {

            var taskWithPoll = _taskCampaignDao.GetForProfile(idTask, idAccount);
            if (taskWithPoll == null)
            {
                return null;
            }

#if DEBUG
            var myWatch = new Stopwatch();
            myWatch.Start();

#endif
            taskWithPoll.ServiceCollection = GetServiceListFromCampaign(taskWithPoll.IdCampaign, idAccount, idTask);

#if DEBUG
            myWatch.Stop();
#endif
            taskWithPoll = AnswerTheQuestionsFromTaskPoll(idTask, taskWithPoll);
            taskWithPoll.BranchImages = GetImagesTask(idAccount, taskWithPoll);
            taskWithPoll.IdTaskNotImplemented = taskWithPoll.IdStatusTask;
#if DEBUG
            myWatch.Stop();
#endif

            return taskWithPoll;
        }

        public List<MyTaskServicesViewModel> GetSectionsPollService(Guid IdCampaign, Guid idAccount)
        {

            List<MyTaskServicesViewModel> taskWithPoll = new List<MyTaskServicesViewModel>();
            if (taskWithPoll == null)
            {
                return null;
            }

#if DEBUG
            var myWatch = new Stopwatch();
            myWatch.Start();
#endif
            taskWithPoll = GetServiceListFromCampaignGeo(IdCampaign, idAccount);

#if DEBUG
            myWatch.Stop();
#endif

#if DEBUG
            myWatch.Stop();
#endif

            return taskWithPoll;
        }




        public object GetBranchApi(Guid IdAccount)
        {
            var result = _branchDao.GetAllBranches().Where(x => x.IdAccount == IdAccount).ToList();
            return result;

        }

        private List<BranchImages> GetImagesTask(Guid idAccount, MyTaskViewModel taskWithPoll)
        {
            var images =
                _branchImageBusiness.GetBranchesImagesList(taskWithPoll.IdBranch, idAccount, taskWithPoll.IdCampaign);

            foreach (var service in taskWithPoll.ServiceCollection)
            {
                foreach (var section in service.ServiceDetailCollection)
                {
                    images.AddRange(
                        section.QuestionCollection.Where(
                                q => q.CodeTypePoll == CTypePoll.Image && !string.IsNullOrEmpty(q.Answer))
                            .Select(q => new BranchImages()
                            {
                                NameFile = q.Title,
                                UrlImage = q.Answer
                            })
                    );
                }
            }
            return images;
        }

        private MyTaskViewModel AnswerTheQuestionsFromTaskPoll(Guid idTask, MyTaskViewModel taskWithPoll)
        {
            var answers = _answerDao.GetAllAnswers(idTask);



            taskWithPoll.ServiceCollection.AsParallel()
                .ForAll(s =>
                {
                    s.ServiceDetailCollection =
                        _serviceDetailBusiness.GetAnsweredSections(s.ServiceDetailCollection, answers);
                });
            return taskWithPoll;
        }

        public TaskRegisterViewModel GetTask(Guid? idCampaign, Guid idTask, Guid idAccount)
        {
            var itemResult = new TaskRegisterViewModel();

            //Creo Tarea
            if (idTask == Guid.Empty && idCampaign != null)
            {
                var campaign = _campaignDao.GetOne(idCampaign.Value, idAccount);
                itemResult.CampaignName = campaign.Name;
                itemResult.IdCampaign = idCampaign.Value;
            }
            //Recupero Tarea
            else if (idTask != Guid.Empty)
            {
                var task = _taskCampaignDao.GetTaskByIdForRegisterPage(idTask, idAccount);
                var campaign = _campaignDao.GetOne(task.IdCampaign, idAccount);
                itemResult = ConvertTask.ToTaskRegisterViewModel(task);
                itemResult.CampaignName = campaign.Name;
                itemResult.IdCampaign = task.IdCampaign;
            }
            //Mala Invocación de método
            else
            {
                throw new ExceptionMardis("Se ha invocado de manera incorrecta a la tarea");
            }

            return itemResult;
        }

        public bool Save(TaskRegisterViewModel model, Guid idAccount)
        {
            var task = ConvertTask.FromTaskRegisterViewModel(model);
            if (model.Id == Guid.Empty)
            {
                var status = _statusTaskBusiness.GeStatusTaskByName(CTask.StatusPending);
                task.IdStatusTask = status.Id;
            }

            task.IdAccount = idAccount;
            task.Branch = null;
            task.Campaign = null;
            task.Merchant = null;
            task.Answers = null;

            if (string.IsNullOrEmpty(task.Code))
            {
                var code = _sequenceBusiness.NextSequence(CTask.SequenceCode, idAccount).ToString();
                task.Code = code;
            }

            _taskCampaignDao.InsertOrUpdate(task);
            //SaveTask(task, idAccount);
            return true;
        }

        public void SaveAnsweredPoll(MyTaskViewModel model, Guid idAccount, Guid idProfile, Guid idUser)
        {
            try
            {
                SaveBranchData(model, idAccount);
                CleanAnswers(model);

                foreach (var service in model.ServiceCollection)
                {
                    foreach (var serviceDetail in service.ServiceDetailCollection)
                    {
                        CreateAnswer(model, idAccount, serviceDetail);

                        if (serviceDetail.Sections != null)
                        {
                            foreach (var section in serviceDetail.Sections)
                            {
                                CreateAnswer(model, idAccount, section);
                            }
                        }

                    }
                }

                FinalizeTask(model, idAccount, idProfile, idUser);
            }
            catch (Exception ex)
            {
                string resultado = ex.Message;
                throw;
            }
           
        }

        private void FinalizeTask(MyTaskViewModel model, Guid idAccount, Guid idProfile, Guid idUser)
        {
            var profile = _profileDao.GetById(idProfile);

            switch (_typeUserBusiness.Get(profile.IdTypeUser).Name)
            {
                case CTypePerson.PersonMerchant:
                case CTypePerson.PersonSupervisor:
                case CTypePerson.PersonSystem:
                    _taskCampaignDao.ImplementTask(model.IdTask, _statusTaskBusiness.GeStatusTaskByName(CTask.StatusImplemented).Id,
                        idAccount);
                    break;
                case CTypePerson.PersonValidator:
                    _taskCampaignDao.ValidateTask(model.IdTask, _statusTaskBusiness.GeStatusTaskByName(CTask.StatusImplemented).Id,
                        idAccount, idUser);
                    break;
            }
        }

        private void CreateAnswer(MyTaskViewModel model, Guid idAccount, MyTaskServicesDetailViewModel serviceDetail)
        {
            foreach (var question in serviceDetail.QuestionCollection)
            {
                if (question.IdQuestionDetail == Guid.Empty && string.IsNullOrEmpty(question.Answer))
                {
                    continue;
                }
                if (question.IdQuestionDetail != Guid.Empty ||
                    (question.CodeTypePoll == CTypePoll.Open && !string.IsNullOrEmpty(question.Answer)))
                {
                    var answer = //_answerDao.GetAnswerValueByQuestion(question.Id, model.IdTask, idAccount) ??
                                 CreateAnswer(model, idAccount, question, serviceDetail);

                    CreateAnswerDetail(answer, question);
                }
            }
        }

        private void CreateAnswerDetail(Answer answer, MyTaskQuestionsViewModel question)
        {
            var answerDetail =
                new AnswerDetail()
                {
                    DateCreation = DateTime.Now,
                    IdAnswer = answer.Id,
                    CopyNumber = question.CopyNumber,
                    StatusRegister = CStatusRegister.Active
                };

            if (question.IdQuestionDetail != Guid.Empty)
            {
                answerDetail.IdQuestionDetail = question.IdQuestionDetail;
            }

            if (question.CodeTypePoll == CTypePoll.Open)
            {
                answerDetail.AnswerValue = question.Answer;
            }

            _answerDetailDao.InsertOrUpdate(answerDetail);
        }

        private Answer CreateAnswer(MyTaskViewModel model, Guid idAccount, MyTaskQuestionsViewModel question,
            MyTaskServicesDetailViewModel serviceDetail)
        {
            var answer = new Answer()
            {
                IdAccount = idAccount,
                IdMerchant = model.IdMerchant,
                IdQuestion = question.Id,
                IdServiceDetail = serviceDetail.Id,
                IdTask = model.IdTask,
                DateCreation = DateTime.Now,
                StatusRegister = CStatusRegister.Active
            };

            answer = _answerDao.InsertOrUpdate(answer);
            return answer;
        }

        private void CleanAnswers(MyTaskViewModel model)
        {
            Context.AnswerDetails.RemoveRange(Context.AnswerDetails.Where(a => a.Answer.IdTask == model.IdTask));
            Context.Answers.RemoveRange(Context.Answers.Where(a => a.IdTask == model.IdTask));
            Context.SaveChanges();
        }

        private void SaveBranchData(MyTaskViewModel model, Guid idAccount)
        {
            var branch = _branchDao.GetOne(model.IdBranch, idAccount);
            var person = _personDao.GetOne(branch.IdPersonOwner);

            branch = ConvertBranch.FromMyTaskViewModel(model, branch);
            person = ConvertPerson.FromMyTaskViewModel(model, person);

            _branchDao.InsertOrUpdate(branch);
            _personDao.InsertOrUpdate(person);
        }

        public bool AddSection(MyTaskViewModel model, Guid idAccount, Guid idProfile, Guid idUser, Guid idseccion)
        {

            SaveAnsweredPoll(model, idAccount, idProfile, idUser);

            var sections = _serviceDetailTaskBusiness.GetSections(model.IdTask, idseccion, idAccount);

            if (!sections.Any())
            {
                _serviceDetailTaskBusiness.AddSection(idseccion, model.IdTask);
                //model.ServiceCollection.Add(_serviceDetailTaskBusiness.AddSection(idseccion, model.IdTask).ServiceDetail);
            }

            _serviceDetailTaskBusiness.AddSection(idseccion, model.IdTask);
            return true;
        }

        public List<MyTaskServicesViewModel> GetServiceListFromCampaign(Guid idCampaign, Guid idAccount, Guid idTask)
        {
            int numero_seccion = 0;
            var servicesParalel = new ConcurrentBag<MyTaskServicesViewModel>();
            var campaignServices = _redisCache.Get<List<MyTaskServicesViewModel>>("CampaignServices:" + idCampaign);
            string idseccion = "";



            if (campaignServices == null || idseccion != "")
            {
                var services = _campaignServicesDao.GetCampaignServicesByCampaign(idCampaign, idAccount);

                services.AsParallel()
                    .ForAll(s =>
                    {
                        servicesParalel.Add(new MyTaskServicesViewModel()
                        {
                            Code = s.Service.Code,
                            Id = s.Service.Id,
                            Name = s.Service.Name,
                            Template = s.Service.Template,
                            ServiceDetailCollection = GetSectionsFromService(s.IdService, idAccount, idTask)
                        });
                    });
                campaignServices = servicesParalel.ToList();

                _redisCache.Set("CampaignServices:" + idCampaign, campaignServices);
            }
            //agregar seccion si es dinamica
            if (campaignServices != null)
            {
                int camp = 0;
                foreach (var q in campaignServices)
                {
                    var s = q.ServiceDetailCollection.Where(x => x.IsDynamic == true).ToList();
                    if (s != null)
                    {
                        foreach (var a in s)
                        {
                            var preguntas = _questionDao.GetQuestion(a.Id).ToList();
                            numero_seccion = 0;

                            foreach (var qtarea in preguntas)
                            {
                                var respuestas = _answerDao.GetAnswerListByQuestionAccount(qtarea.Id, idAccount, idTask).ToList().Count;
                                if (respuestas > 1)
                                {
                                    numero_seccion = respuestas;
                                }

                            }
                            if (numero_seccion > 1)
                            {

                                int numero = 0;
                                foreach (var n in q.ServiceDetailCollection.Where(x => x.Id == a.Id).ToList())
                                {
                                    if (numero > 0)
                                    {
                                        q.ServiceDetailCollection.Remove(n);
                                    }
                                    else
                                    {
                                        foreach (var ques in n.QuestionCollection)
                                        {
                                            ques.sequence = 0;
                                        }
                                    }
                                    numero++;
                                }
                                numero = 0;
                                for (int numRep = 0; numRep < numero_seccion - 1; numRep++)
                                {

                                    MyTaskServicesDetailViewModel SeccionInsertar = new MyTaskServicesDetailViewModel();
                                    SeccionInsertar = GetSectionsFromServiceID(q.Id, idAccount, a.Id, numero + 1);
                                    q.ServiceDetailCollection.Insert(SeccionInsertar.Order, SeccionInsertar);
                                    numero++;
                                }
                            }
                            else
                            {
                                var num = q.ServiceDetailCollection.Where(x => x.Id == a.Id).ToList();
                                int numero = 0;
                                foreach (var n in q.ServiceDetailCollection.Where(x => x.Id == a.Id).ToList())
                                {
                                    if (numero > 0)
                                    {
                                        q.ServiceDetailCollection.Remove(n);
                                    }
                                    numero++;
                                }
                            }
                        }

                    }
                    campaignServices[camp].ServiceDetailCollection.OrderBy(x => x.Order).ToList();
                    camp++;
                }

            }
            return campaignServices;
        }
        public List<MyTaskServicesViewModel> GetServiceListFromCampaignGeo(Guid idCampaign, Guid idAccount)
        {
            var servicesParalel = new ConcurrentBag<MyTaskServicesViewModel>();
            var campaignServices = _redisCache.Get<List<MyTaskServicesViewModel>>("CampaignServices:" + idCampaign);

            if (campaignServices == null)
            {
                var services = _campaignServicesDao.GetCampaignServicesByCampaign(idCampaign, idAccount);

                services.AsParallel()
                    .ForAll(s =>
                    {
                        servicesParalel.Add(new MyTaskServicesViewModel()
                        {
                            Code = s.Service.Code,
                            Id = s.Service.Id,
                            Name = s.Service.Name,
                            Template = s.Service.Template,
                            ServiceDetailCollection = GetSectionsFromServiceGeo(s.IdService, idAccount)
                        });
                    });
                campaignServices = servicesParalel.ToList();

                _redisCache.Set("CampaignServices:" + idCampaign, campaignServices);
            }

            return campaignServices;
        }

        public List<MyTaskServicesDetailViewModel> GetSectionsFromService(Guid idService, Guid idAccount, Guid idTask)
        {
            var sections =
                Mapper.Map<List<MyTaskServicesDetailViewModel>>(_serviceDetailDao.GetServiceDetailsFromService(idService, idAccount, idTask));


            sections.AsParallel().ForAll(s =>
            {
                s.QuestionCollection = GetQuestionsFromSection(s.Id);
                s.Sections.AsParallel().ForAll(sc => sc.QuestionCollection = GetQuestionsFromSection(sc.Id));
            });

            return sections;
        }
        public MyTaskServicesDetailViewModel GetSectionsFromServiceID(Guid idService, Guid idAccount, Guid idservicedetail, int Orden)
        {
            var sections = Mapper.Map<MyTaskServicesDetailViewModel>(_serviceDetailDao.GetServiceDetailsFromServiceID(idService, idAccount, idservicedetail));


            if (sections != null)
            {
                sections.QuestionCollection = GetQuestionsFromSectionID(sections.Id, Orden);
                sections.Sections.AsParallel().ForAll(sc => sc.QuestionCollection = GetQuestionsFromSectionID(sc.Id, Orden));
            }
            return sections;
        }
        public List<MyTaskServicesDetailViewModel> GetSectionsFromServiceGeo(Guid idService, Guid idAccount)
        {
            var sections =
                Mapper.Map<List<MyTaskServicesDetailViewModel>>(_serviceDetailDao.GetServiceDetailsFromServiceGeo(idService, idAccount));


            sections.AsParallel().ForAll(s =>
            {
                s.QuestionCollection = GetQuestionsFromSection(s.Id);
                s.Sections.AsParallel().ForAll(sc => sc.QuestionCollection = GetQuestionsFromSection(sc.Id));
            });

            return sections;
        }

        /// <summary>
        /// Obtener Preguntas con sus respectivas opciones de respuestas mediante el código de la sección
        /// </summary>
        /// <param name="idSection">Identificador de la sección</param>
        /// <returns>Listado de preguntas con opciones de respuestas por Sección</returns>
        public List<MyTaskQuestionsViewModel> GetQuestionsFromSection(Guid idSection)
        {
            var questions = Mapper.Map<List<MyTaskQuestionsViewModel>>(_questionDao.GetCompleteQuestion(idSection).Result);

            /* foreach (var q in questions)
             {
                 q.sequence = 1;
                 resultado.Add(q);
             }*/

            return questions.OrderBy(q => q.Order).ToList();
        }
        public List<MyTaskQuestionsViewModel> GetQuestionsFromSectionID(Guid idSection, int orden)
        {
            var questions = Mapper.Map<List<MyTaskQuestionsViewModel>>(_questionDao.GetCompleteQuestion(idSection).Result);

            foreach (var q in questions)
            {
                q.sequence = orden;

            }

            return questions.OrderBy(q => q.Order).ToList();
        }
    }
}
