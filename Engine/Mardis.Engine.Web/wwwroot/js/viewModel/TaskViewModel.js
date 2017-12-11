guidEmpty = "00000000-0000-0000-0000-000000000000";

var vueVM;

function LoadTaskById(idTask) {
    $.blockUI({ message: "cargando..." });
    $.ajax({
        type: "GET",
        url: "/Task/Get",
        // async: false,
        data: {
            idTask: idTask
        },
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (data) {
            if (data) {
                ApplyBindingTaskService(data);
            } else {
                alert("Error! no se ha encontrado la tarea" + error);
                window.location.href = "/Task/MyTasks";
            }
        },
        error: function (error) {
            console.log(error);
            alert("Error! no se ha encontrado la tarea" + error);
            window.location.href = "/Task/MyTasks";
        }
    });
}




function ApplyBindingTaskService(data) {
    vueVM = new Vue({
        el: "#divPoll",
        data: {
            poll: data
        },
        methods: {
            keymonitor: function (event) {
                var charCode = (event.which) ? event.which : event.keyCode
                if (charCode > 31 && (charCode < 48 || charCode > 57)) {
                    return false;
                }
                console.log(charCode);
                return true;
            },
            moment: function () {
                return moment();
            },
            openModal: function () {
                return openModal();
            },
            currentSlide: function (data) {
                return currentSlide(data);
            },
            Save: function () {
                return Save();
            }
        }
    });

    $.unblockUI();
}

function BuscarPregunta(servicio, idpregunta) {
    if (servicio != null) {
        var preguntas = servicio.ServiceDetailCollection[s];
        var seccion = servicio.ServiceDetailCollection[s].Sections;
        if (preguntas != null) {
            for (var p = 0; p <= preguntas.QuestionCollection.length - 1; p++) {
                var preguntasopciones = preguntas.QuestionCollection[p].QuestionDetailCollection;

                if (preguntas.QuestionCollection[p].CodeTypePoll == "ONE") {
                    if (preguntas.QuestionCollection[p].IdQuestionDetail == "00000000-0000-0000-0000-000000000000") {
                        return "vacia";
                    }
                } else {
                    if (preguntas.QuestionCollection[p].Answer == null) {
                        return "vacia";
                    }
                }
                if (seccion != null) {
                    for (var sec = 0; sec <= seccion.length - 1; sec++) {
                        var preguntasSec = seccion[sec].QuestionCollection;
                        for (var pc = 0; pc <= preguntasSec.length - 1; pc++) {
                            if (preguntasSec[pc].AnswerRequired == true) {
                                if (preguntasSec[pc].CodeTypePoll == "ONE") {
                                    if (preguntasSec[pc].IdQuestionDetail == "00000000-0000-0000-0000-000000000000") {
                                        return "vacia";
                                    }
                                } else {
                                    if (preguntasSec[pc].Answer == null) {
                                        return "vacia";
                                    }
                                }
                            }

                        }

                    }
                }
            }
        }
    }
}
function ValidarPreguntas() {
    var mensaje = "";
    for (var i = 0; i <= vueVM.$data.poll.ServiceCollection.length - 1; i++) {

        var servicio = vueVM.$data.poll.ServiceCollection[i];
        if (servicio != null) {
            for (var s = 0; s <= servicio.ServiceDetailCollection.length - 1; s++) {
                var preguntas = servicio.ServiceDetailCollection[s];
                var seccion = servicio.ServiceDetailCollection[s].Sections;
                if (preguntas != null) {
                    for (var p = 0; p <= preguntas.QuestionCollection.length - 1; p++) {
                        if (preguntas.QuestionCollection[p].AnswerRequired == true) {
                            if (preguntas.QuestionCollection[p].CodeTypePoll == "ONE") {
                                if (preguntas.QuestionCollection[p].IdQuestionDetail == "00000000-0000-0000-0000-000000000000") {
                                    mensaje = mensaje + "\n" + preguntas.QuestionCollection[p].Title;
                                    $("#" + preguntas.QuestionCollection[p].Id).focus();
                                } else {
                                    if (preguntas.QuestionCollection[p].IdQuestionRequired != null && preguntas.QuestionCollection[p].IdQuestionRequired != '') {

                                        var str = preguntasopciones[qd].IdQuestionRequired;
                                        if (str.indexOf("&") > -1) {
                                            var res = str.split("&");
                                            var validalogica = "";
                                            for (var j = 0; j < res.length; j++) {
                                                if (res[j] != '') {
                                                    var idque = res[j]
                                                    validalogica = BuscarPregunta(servicio.ServiceDetailCollection[s], res[j])
                                                }

                                            }
                                            if (validalogica != "") {
                                                mensaje = mensaje + "\n Seleccione una opcion de " + servicio.ServiceDetailCollection[s].Title;
                                                $("#" + preguntas.QuestionCollection[p].Id).focus();
                                            }
                                        }
                                    }
                                }
                            } else {
                                if (preguntas.QuestionCollection[p].Answer == null) {
                                    mensaje = mensaje + "\n" + preguntas.QuestionCollection[p].Title;
                                    $("#" + preguntas.QuestionCollection[p].Id).focus();
                                }
                            }
                        }
                    }
                }
                if (seccion != null) {
                    for (var sec = 0; sec <= seccion.length - 1; sec++) {
                        var preguntasSec = seccion[sec].QuestionCollection;
                        for (var pc = 0; pc <= preguntasSec.length - 1; pc++) {
                            if (preguntasSec[pc].AnswerRequired == true) {
                                if (preguntasSec[pc].CodeTypePoll == "ONE") {
                                    if (preguntasSec[pc].IdQuestionDetail == "00000000-0000-0000-0000-000000000000") {
                                        mensaje = mensaje + "\n" + preguntasSec[pc].Title + " " + seccion[sec].SectionTitle;
                                        $("#" + preguntasSec[pc].Id).focus();
                                    } else {
                                        if (preguntasSec[pc].IdQuestionRequired != null && preguntasSec[pc].IdQuestionRequired != '') {

                                            var str = preguntasSec[pc].IdQuestionRequired;
                                            if (str.indexOf("&") > -1) {
                                                var res = str.split("&");
                                                var validalogica = "";
                                                for (var j = 0; j < res.length; j++) {
                                                    if (res[j] != '') {
                                                        var idque = res[j]
                                                        validalogica = BuscarPregunta(seccion[sec], res[j])
                                                    }

                                                }
                                                if (validalogica != "") {
                                                    mensaje = mensaje + "\n Seleccione una opcion de " + seccion[sec].Title;
                                                    $("#" + preguntasSec[pc].Id).focus();
                                                }
                                            }
                                        }
                                    }
                                } else {
                                    if (preguntasSec[pc].Answer == null) {
                                        mensaje = mensaje + "\n" + preguntasSec[pc].Title + " " + seccion[sec].SectionTitle;
                                        $("#" + preguntasSec[pc].Id).focus();
                                    }
                                }
                            }

                        }

                    }
                }

            }
        }

    }
    return mensaje;
}

function Save() {
    var imgsrc = 'http://www.google.es/intl/en_com/images/logo_plain.png';
    var img = new Image();
    img.onerror = function () {
        alert("No hay conexion a internet.");
    }
    img.onload = function () {
        //alert("Hay conexion a internet.");
    }
    img.src = imgsrc;
    $.blockUI({ message: "" });
    var resulvalidacionC = "";
    var resulvalidacion = "";
    resulvalidacionC = ValidarPreguntas();
    if (resulvalidacionC != "") {
        resulvalidacion = "Llenar Campos Obligatorios..!!" + "\n" + resulvalidacionC;
    }
    if (resulvalidacion == "") {
        if (navigator.onLine) {
            $.ajax({
                url: "/Task/Save",
                type: "post",
                data: {
                    task: ko.toJSON(vueVM.$data.poll)
                },
                success: function (data) {
                    if (data) {
                        bootbox.alert("Registros Actualizados Satisfactoriamente");
                        window.location.href = "/Task/MyTasks";
                        alert("Error! no se ha encontrado la tarea" + data);
                    } else {
                        bootbox.alert("Error al tratar de Grabar su encuesta");
                        window.location.href = "/Task/MyTasks";
                        alert("Error! no se ha encontrado la tarea" + data);
                    }
                },
                error: function (xhr, ajaxOptions, thrownError) {
                    bootbox.alert("Error al tratar de Grabar su encuesta " + thrownError);
                    window.location.href = "/Task/MyTasks";
                    $.unblockUI();
                }
            });
            window.location.href = "/Task/MyTasks";
        } else {
            alert('Sin Conexión...Intente mas tarde.')
            $.unblockUI();
        }
    } else {
        alert(resulvalidacion);
        $.unblockUI();
    }
}

function ChangeStatusNotImplemented(element) {
    $.blockUI({ message: "" });
    var idTask = $(element).data("idtask");
    var idReason = $("#ddlReasons").val();
    var comment = $("#txtObservation").val();
    $.ajax({
        url: "/Task/ChangeStatusNotImplemented",
        type: "POST",
        data: {
            idTask: idTask,
            idReason: idReason,
            comment: comment
        },
        success: function (data) {

            if (data) {
                bootbox.alert("Registros Actualizados Satisfactoriamente");
                window.location.href = "/Task/MyTasks";
            } else {
                bootbox.alert("Existío un error, Vuelva a intentarlo");
            }

        },
        complete: function (data) {
            $.unblockUI();
        },
        error: function (error) {
            console.log(error);
            $.unblockUI();
        }
    });
}
function AddSection(isSectionAfter, isSectionBefore, element) {
    $.blockUI({ message: "" });
    //Caso 1: Es una nueva sección
    if (!isSectionAfter && !isSectionBefore) {

        $.get("/Service/AddSection", function (data) {
            var curr = $(element).data("ordersection");
            var sect = $(element).data("order");
            var Section = $.extend(true, {}, vueVM.$data.poll.ServiceCollection[0].ServiceDetailCollection[sect]);
            vueVM.$data.poll.ServiceCollection[0].ServiceDetailCollection.push(Section);
        });

        $.unblockUI();
    }

    //Caso 2: Ingreso una Sección antes de una seccion existente
    if (!isSectionAfter && isSectionBefore) {

        $.get("/Service/AddSection", function (data) {
            var curr = $(element).data("order");
            vueVM.$data.poll.ServiceCollection[0].ServiceDetailCollection.push(data);
        });

        $.unblockUI();
    }

    ////Caso 3: Ingreso de una sección despues de una sección existente
    if (isSectionAfter && !isSectionBefore) {

        $.get("/Service/AddSection", function (data) {
            var curr = $(element).data("order");
            vueVM.$data.poll.ServiceCollection[0].ServiceDetailCollection.push(data);
        });

        $.unblockUI();
    }




}
function ObtenerPregunta(idquestiondetail, idquestion) {
    for (var i = 0; i <= vueVM.$data.poll.ServiceCollection.length - 1; i++) {

        var servicio = vueVM.$data.poll.ServiceCollection[i];
        if (servicio != null) {
            for (var s = 0; s <= servicio.ServiceDetailCollection.length - 1; s++) {
                var preguntas = servicio.ServiceDetailCollection[s];
                var seccion = servicio.ServiceDetailCollection[s].Sections;
                if (preguntas != null) {
                    for (var p = 0; p <= preguntas.QuestionCollection.length - 1; p++) {
                        if (preguntas.QuestionCollection[p].Id == idquestion) {
                            var preguntasopciones = preguntas.QuestionCollection[p].QuestionDetailCollection
                            for (var qd = 0; qd <= preguntasopciones.length; qd++) {
                                if (preguntasopciones[qd].Id == idquestiondetail) {

                                    if (preguntasopciones[qd].IdQuestionLink != null) {
                                        $("#" + preguntasopciones[qd].IdQuestionLink).focus();
                                        if (preguntasopciones[qd].IdQuestionRequired != null && preguntasopciones[qd].IdQuestionRequired != '') {
                                            ColocarObligatorios(preguntasopciones[qd].IdQuestionRequired, idquestion);
                                        } else {
                                            LimpiarObligatorias(idquestion, preguntasopciones[qd].Id);
                                        }
                                        return;
                                    }

                                }
                            }
                        }

                    }
                }
                if (seccion != null) {
                    for (var sec = 0; sec <= seccion.length - 1; sec++) {
                        var preguntasSec = seccion[sec].QuestionCollection;
                        for (var pc = 0; pc <= preguntasSec.length - 1; pc++) {
                            if (preguntasSec[pc].Id == idquestion) {
                                var preguntasopciones = preguntasSec[pc].QuestionDetailCollection
                                for (var qd = 0; qd <= preguntasopciones.length; qd++) {
                                    if (preguntasopciones[qd].Id == idquestiondetail) {

                                        if (preguntasopciones[qd].IdQuestionLink != null) {
                                            $("#" + preguntasopciones[qd].IdQuestionLink).focus();
                                            if (preguntasopciones[qd].IdQuestionRequired != null && preguntasopciones[qd].IdQuestionRequired != '') {
                                                ColocarObligatorios(preguntasopciones[qd].IdQuestionRequired, idquestion);
                                            } else {
                                                LimpiarObligatorias(idquestion, preguntasopciones[qd].Id);
                                            }
                                            return;
                                        }

                                    }
                                }
                            }
                        }

                    }
                }

            }
        }
    }




}
function ColocarObligatorios(idquestionobligatorias, idquestiondetail) {
    var str = idquestionobligatorias;
    if (str.indexOf("&") == -1) {
        var res = str.split("|");
        for (var j = 0; j < res.length; j++) {
            for (var i = 0; i <= vueVM.$data.poll.ServiceCollection.length - 1; i++) {

                var servicio = vueVM.$data.poll.ServiceCollection[i];
                if (servicio != null) {
                    for (var s = 0; s <= servicio.ServiceDetailCollection.length - 1; s++) {
                        var preguntas = servicio.ServiceDetailCollection[s];
                        var seccion = servicio.ServiceDetailCollection[s].Sections;
                        if (preguntas != null) {
                            for (var p = 0; p <= preguntas.QuestionCollection.length - 1; p++) {

                                if (preguntas.QuestionCollection[p].Id == res[j].toLowerCase()) {

                                    preguntas.QuestionCollection[p].AnswerRequired = true;
                                }


                            }

                        }

                        if (seccion != null) {
                            for (var sec = 0; sec <= seccion.length - 1; sec++) {
                                var preguntasSec = seccion[sec].QuestionCollection;
                                for (var pc = 0; pc <= preguntasSec.length - 1; pc++) {
                                    if (preguntasSec[pc].Id == res[j].toLowerCase()) {

                                        preguntasSec[pc].AnswerRequired = true;
                                    }
                                }

                            }
                        }

                    }
                }
            }
        }
    }
}

function LimpiarObligatorias(idquestion, idquestiondetail) {
    for (var i = 0; i <= vueVM.$data.poll.ServiceCollection.length - 1; i++) {

        var servicio = vueVM.$data.poll.ServiceCollection[i];
        if (servicio != null) {
            for (var s = 0; s <= servicio.ServiceDetailCollection.length - 1; s++) {
                var preguntas = servicio.ServiceDetailCollection[s];
                var seccion = servicio.ServiceDetailCollection[s].Sections;
                if (preguntas != null) {
                    for (var p = 0; p <= preguntas.QuestionCollection.length - 1; p++) {
                        if (preguntas.QuestionCollection[p].Id == idquestion) {
                            var preguntasopciones = preguntas.QuestionCollection[p].QuestionDetailCollection
                            for (var qd = 0; qd <= preguntasopciones.length; qd++) {
                                if (preguntasopciones[qd] != undefined) {
                                    if (preguntasopciones[qd].Id != idquestiondetail) {

                                        if (preguntasopciones[qd].IdQuestionRequired != null && preguntasopciones[qd].IdQuestionRequired != '') {

                                            var str = preguntasopciones[qd].IdQuestionRequired;
                                            var res = str.split("|");
                                            for (var j = 0; j < res.length; j++) {
                                                var idque = res[j]
                                                limpiarQuestion(idque);

                                            }
                                        }

                                    }

                                }
                            }
                        }

                    }
                }
                if (seccion != null) {
                    for (var sec = 0; sec <= seccion.length - 1; sec++) {
                        var preguntasSec = seccion[sec].QuestionCollection;
                        for (var pc = 0; pc <= preguntasSec.length - 1; pc++) {
                            if (preguntasSec[pc].Id == idquestion) {
                                var preguntasopciones = preguntasSec[pc].QuestionDetailCollection
                                for (var qd = 0; qd <= preguntasopciones.length; qd++) {
                                    if (preguntasopciones[qd] != undefined) {
                                        if (preguntasopciones[qd].Id != idquestiondetail) {

                                            if (preguntasopciones[qd].IdQuestionRequired != null && preguntasopciones[qd].IdQuestionRequired != '') {
                                                var str = preguntasopciones[qd].IdQuestionRequired;
                                                var res = str.split("|");
                                                for (var j = 0; j < res.length; j++) {
                                                    var idque = res[j]
                                                    limpiarQuestion(idque);

                                                }

                                            }

                                        }
                                    }
                                }
                            }
                        }

                    }
                }

            }
        }
    }

}
function limpiarQuestion(idquestion) {
    for (var i = 0; i <= vueVM.$data.poll.ServiceCollection.length - 1; i++) {

        var servicio = vueVM.$data.poll.ServiceCollection[i];
        if (servicio != null) {
            for (var s = 0; s <= servicio.ServiceDetailCollection.length - 1; s++) {
                var preguntas = servicio.ServiceDetailCollection[s];
                var seccion = servicio.ServiceDetailCollection[s].Sections;
                if (preguntas != null) {
                    for (var p = 0; p <= preguntas.QuestionCollection.length - 1; p++) {
                        if (preguntas.QuestionCollection[p].Id == idquestion.toLowerCase()) {

                            preguntas.QuestionCollection[p].AnswerRequired = false;
                        }


                    }

                }
            }
            if (seccion != null) {
                for (var sec = 0; sec <= seccion.length - 1; sec++) {
                    var preguntasSec = seccion[sec].QuestionCollection;
                    for (var pc = 0; pc <= preguntasSec.length - 1; pc++) {
                        if (preguntasSec[pc].Id == idquestion.toLowerCase()) {
                            preguntasSec[pc].AnswerRequired = false;
                        }
                    }

                }
            }

        }
    }

}
