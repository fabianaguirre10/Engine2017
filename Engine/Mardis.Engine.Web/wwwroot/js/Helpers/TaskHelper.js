var OpenQuestionResult = [];
var OneQuestionResult = [];
var ManyQuestionResult = [];

function SearchBranches() {
    var countryCode = $("#selCountries").val();
    var provinceCode = $("#selProvinces").val();
    var districtCode = $("#selDistricts").val();
    var documentType = $("#dllDocumentType").val();
    var document = $("#txtIdentification").val();
    var nameBranch = $("#txtNameBranch").val();
    var ownerName = $("#txtOwner").val();
    var codeBranch = $("#txtCode").val();

    $.blockUI({ message:"" });

    $.ajax({
        type: "GET",
        url: "/Branch/SearchBranches",
        data: {
            idCountry: countryCode,
            idProvince: provinceCode,
            idDistrict: districtCode,
            documentType: documentType,
            document: document,
            nameBranch: nameBranch,
            ownerName: ownerName,
            codeBranch: codeBranch
        },
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (data) {
            modelView.BranchesResult(data);
            $.unblockUI();
            //ApplyBindingRegisterTask(data);
        },
        error: function () {
            $.unblockUI();
            alert("error");
        }
    });
}

function ValidateSearch() {

    if (modelView.branchSelected().length !== 1) {
        bootbox.alert("Debe seleccionar exclusivamente un Local");
    } else {
        modelView.IdBranch = modelView.branchSelected()[0];
        $.ajax({
            type: "GET",
            url: "/Branch/GetBranchById",
            data: {
                idBranch: modelView.IdBranch
            },
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (data) {
                $("#lblNameBranch").html(data.Name);
                $("#divEscogerLocal").modal("hide");
                $("#txtBranch").val(data.Id);
            },
            error: function () {
                alert("error");
            }
        });
    }
}

var emptyGuid = "00000000-0000-0000-0000-000000000000";
var selectedItems = [];

function LoadCountries() {
    $.getJSON("/ServicesLocalization/GetAllCountries", {}, function (data) {
        var items = "<option>--Seleccionar--</option>";
        $("#selCountries").empty();
        $.each(data, function (i, country) {
            items += "<option value='" + country.Id + "'>" + country.Name + "</option>";
        });
        $("#selCountries").html(items);
    });
}

$("#selCountries").change(function () {
    $.blockUI({ message: "" });
    var url = "/ServicesLocalization/GetProvincesByCountryId";
    var idCountry = $("#selCountries").val();
    $.getJSON(url, { countryId: idCountry }, function (data) {
        var items = "<option>--Seleccionar--</option>";
        $("#selProvinces").empty();
        $("#selDistricts").html(items);
        $("#selParishes").html(items);
        $("#selSectors").html(items);
        $.each(data, function (i, province) {
            items += "<option value='" + province.Id + "'>" + province.Name + "</option>";
        });
        $("#selProvinces").html(items);
        $.unblockUI();
    });
});

$("#selProvinces").change(function () {
    $.blockUI({ message: "" });
    var url = "/ServicesLocalization/GetDistrictsByProvinceId";
    var idProvince = $("#selProvinces").val();
    $.getJSON(url, { idProvince: idProvince }, function (data) {
        var items = "<option>--Seleccionar--</option>";
        $("#selDistricts").empty();
        $("#selParishes").html(items);
        $("#selSectors").html(items);
        $.each(data, function (i, district) {
            items += "<option value='" + district.Id + "'>" + district.Name + "</option>";
        });
        $("#selDistricts").html(items);
        $.unblockUI();
    });
});

function MainTaskViewModel() {
    var self = this;
    self.BranchesResult = ko.observableArray([]);
    self.branchSelected = ko.observableArray();
    self.IdBranch = ko.observable();
}

var modelView = new MainTaskViewModel();
ko.cleanNode("divBranchesResult");
ko.applyBindings(modelView, document.getElementById("divBranchesResult"));

var selectedItems = [];

function SelectItem(element) {
    var id = element.id;
    if (element.checked) {
        selectedItems.push(id);
    } else {
        selectedItems.forEach(function (item, i) {
            if (item === id) {
                selectedItems.splice(i, 1);
            }
        });
    }
}

function DeleteSelection() {
    $.blockUI({ message: "" });

    bootbox.confirm("Esta seguro que desea eliminar?", function (result) {
        if (result) {
            $.ajax({
                url: "/Task/Delete",
                type: "post",
                data: {
                    input: JSON.stringify(selectedItems)
                },
                success: function (data) {

                    if (data) {

                        window.location.href = "/Task/Index";

                    } else {
                        bootbox.alert("Existío un error, Vuelva a intentarlo");
                    }
                    $.unblockUI();
                },
                error: function (xhr) {
                    console.log(xhr);
                    $.unblockUI();
                    //Do Something to handle error
                }
            });
        }
    });
}

function InsertSection(sectionId) {
    $("#txtSection").val(sectionId);
}