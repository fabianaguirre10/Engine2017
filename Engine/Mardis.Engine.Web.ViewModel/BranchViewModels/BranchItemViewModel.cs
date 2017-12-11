using System;

namespace Mardis.Engine.Web.ViewModel.BranchViewModels
{
    public class BranchItemViewModel
    {
        public Guid Id { get; set; }
        public Guid IdAccount { get; set; }

        public string ExternalCode { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }
        public string calle1 { get; set; }

        public string Neighborhood { get; set; }

        public string Reference { get; set; }
      

    }
}