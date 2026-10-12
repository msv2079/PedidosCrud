"use strict";

$(function () {
    const orderModal = new bootstrap.Modal(document.getElementById("order-modal"));
    const deleteModal = new bootstrap.Modal(document.getElementById("delete-modal"));
    const form = $("#order-form");
    const moneyInput = document.getElementById("TotalAmount");
    const currencyFormatter = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });
    const dateFormatter = new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" });
    const statusLabels = { Open: "Aberto", Paid: "Pago", Canceled: "Cancelado" };
    const statusClasses = { Open: "status-open", Paid: "status-paid", Canceled: "status-canceled" };
    let activeTableRequest = null;
    let lastTableResult = { data: [], recordsTotal: 0, recordsFiltered: 0 };
    let selectedOrder = null;
    let isSaving = false;

    Inputmask({ alias: "currency", radixPoint: ",", groupSeparator: ".", digits: 2, digitsOptional: false, rightAlign: false, allowMinus: false, unmaskAsNumber: true, prefix: "", max: 1000000 }).mask(moneyInput);
    $.validator.addMethod("brMoney", function () { const amount = readAmount(); return amount !== null && amount >= 0.01 && amount <= 1000000; }, "Informe um valor entre R$ 0,01 e R$ 1.000.000,00.");
    $(moneyInput).rules("remove", "number range");
    $(moneyInput).rules("add", { brMoney: true });
    $.validator.addMethod("knownStatus", value => Object.hasOwn(statusLabels, value), "Selecione um status válido.");
    $("#Status").rules("add", { knownStatus: true });
    $("#CustomerName, #Description").each(function () { $(this).rules("add", { normalizer: value => value.trim() }); });
    $.fn.dataTable.ext.errMode = "none";

    const table = $("#orders-table").DataTable({
        serverSide: true,
        processing: true,
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100],
        order: [[0, "desc"]],
        layout: { topStart: null, topEnd: null, bottomStart: ["pageLength", "info"], bottomEnd: "paging" },
        ajax: loadTable,
        columns: [
            { data: "id", render: id => `<span class="order-number">#${String(id).padStart(5, "0")}</span>` },
            { data: "customerName", className: "customer-name", render: $.fn.dataTable.render.text() },
            { data: "description", className: "description-cell", render: $.fn.dataTable.render.text() },
            { data: "totalAmount", className: "amount-cell", render: amount => currencyFormatter.format(amount) },
            { data: "status", render: status => `<span class="status-badge ${statusClasses[status] || ""}">${statusLabels[status] || "Desconhecido"}</span>` },
            { data: "createdAt", className: "date-cell", render: date => dateFormatter.format(new Date(date)) },
            { data: null, className: "actions-cell", orderable: false, searchable: false, render: () => '<button type="button" class="action-button edit" aria-label="Editar pedido" title="Editar pedido"><i class="fa-regular fa-pen-to-square" aria-hidden="true"></i></button><button type="button" class="action-button delete" aria-label="Excluir pedido" title="Excluir pedido"><i class="fa-regular fa-trash-can" aria-hidden="true"></i></button>' }
        ],
        language: { processing: "Carregando pedidos...", emptyTable: "Nenhum pedido cadastrado.", zeroRecords: "Nenhum pedido encontrado para esta pesquisa.", info: "_START_–_END_ de _TOTAL_ pedidos", infoEmpty: "0 pedidos", infoFiltered: "(de _MAX_ no total)", lengthMenu: "_MENU_ por página", paginate: { first: "Primeira", previous: "Anterior", next: "Próxima", last: "Última" } }
    });

    async function loadTable(request, callback) {
        if (activeTableRequest) {
            activeTableRequest.abort();
        }

        $("#refresh-status").text("Atualizando...");
        activeTableRequest = $.ajax({ url: "/api/pedidos/datatable", data: request, method: "GET" });
        const currentRequest = activeTableRequest;

        try {
            const result = await currentRequest;
            lastTableResult = result;
            callback(result);
            $("#refresh-dot").removeClass("offline");
            $("#refresh-status").text(`Atualizado às ${new Date().toLocaleTimeString("pt-BR")}`);

            if (request.start > 0 && result.data.length === 0) {
                setTimeout(() => table.page(result.recordsFiltered > 0 ? "last" : 0).draw("page"), 0);
            }
        } catch (error) {
            if (error.statusText === "abort") {
                return;
            }

            if (error.status === 401) {
                window.location.assign("/account/login");
                return;
            }

            callback({ ...lastTableResult, draw: request.draw });
            $("#refresh-dot").addClass("offline");
            $("#refresh-status").text("Falha na atualização. Tentaremos novamente.");
            showMessage("#page-message", "Não foi possível atualizar os pedidos. Os últimos dados recebidos continuam na tabela.", "warning");
        } finally {
            if (activeTableRequest === currentRequest) {
                activeTableRequest = null;
            }
        }
    }

    function readAmount() {
        if (!moneyInput.value.trim()) {
            return null;
        }

        const amount = Number(moneyInput.inputmask.unmaskedvalue());
        return Number.isFinite(amount) ? amount : null;
    }

    async function sendRequest(url, method = "GET", body = null) {
        const headers = { "X-CSRF-TOKEN": form.find('[name="__RequestVerificationToken"]').val() };

        if (body !== null) {
            headers["Content-Type"] = "application/json";
        }

        let response;

        try {
            response = await fetch(url, { method, headers, credentials: "same-origin", body: body === null ? null : JSON.stringify(body) });
        } catch {
            throw { status: 0, detail: "Não foi possível conectar ao servidor. Verifique a conexão e tente novamente." };
        }

        if (response.status === 401) {
            window.location.assign("/account/login");
            throw { status: 401, detail: "Sua sessão expirou. Entre novamente." };
        }

        if (!response.ok) {
            const problem = await response.json().catch(() => ({ detail: "Não foi possível concluir a operação." }));
            throw { ...problem, status: response.status };
        }

        return response.status === 204 ? null : response.json();
    }

    function showMessage(selector, message, type = "danger") {
        $(selector).removeClass("d-none alert-danger alert-success alert-warning").addClass(`alert-${type}`).text(message);
    }

    function resetForm() {
        form[0].reset();
        form.validate().resetForm();
        form.find(".input-validation-error, .is-invalid").removeClass("input-validation-error is-invalid");
        form.find("[data-valmsg-for]").empty();
        $("#order-id, #order-version").val("");
        $("#form-message").addClass("d-none").empty();
        moneyInput.inputmask.setValue("");
        $("#Status").val("Open");
    }

    function showFormError(problem) {
        const fieldErrors = {};

        for (const [key, messages] of Object.entries(problem.errors || {})) {
            const fieldName = key.charAt(0).toUpperCase() + key.slice(1);

            if (form.find(`[name="${fieldName}"]`).length > 0) {
                fieldErrors[fieldName] = messages.join(" ");
            }
        }

        form.validate().showErrors(fieldErrors);
        const message = problem.detail || problem.title || "Verifique os campos informados.";
        showMessage("#form-message", message);

        if (problem.status === 409 || problem.status === 404) {
            table.ajax.reload(null, false);
        }
    }

    function setBusy(modalSelector, busy) {
        $(modalSelector).find("button, input, textarea, select").prop("disabled", busy);
        $(modalSelector).find(".modal-content").LoadingOverlay(busy ? "show" : "hide", { image: "", fontawesome: "fa-solid fa-circle-notch fa-spin", fontawesomeColor: "#1d6455", background: "rgba(255,255,255,0.65)" });
    }

    $("#create-order").on("click", function () {
        resetForm();
        $("#order-modal-title").text("Novo pedido");
        orderModal.show();
    });

    $("#order-modal").on("shown.bs.modal", function () { $("#CustomerName").trigger("focus"); });
    $("#order-modal, #delete-modal").on("hide.bs.modal", function (event) { if (isSaving) { event.preventDefault(); } });

    $("#orders-table").on("click", ".edit", async function () {
        const button = $(this);
        const row = table.row(button.closest("tr")).data();
        button.prop("disabled", true);

        try {
            const order = await sendRequest(`/api/pedidos/${row.id}`);
            resetForm();
            $("#order-modal-title").text(`Editar pedido #${order.id}`);
            $("#order-id").val(order.id);
            $("#order-version").val(order.version);
            $("#CustomerName").val(order.customerName);
            $("#Description").val(order.description);
            moneyInput.inputmask.setValue(order.totalAmount.toFixed(2).replace(".", ","));
            $("#Status").val(order.status);
            orderModal.show();
        } catch (problem) {
            showMessage("#page-message", problem.detail);
            table.ajax.reload(null, false);
        } finally {
            button.prop("disabled", false);
        }
    });

    form.on("submit", async function (event) {
        event.preventDefault();

        if (isSaving || !form.valid()) {
            return;
        }

        const id = $("#order-id").val();
        const input = { customerName: $("#CustomerName").val().trim(), description: $("#Description").val().trim(), totalAmount: readAmount(), status: $("#Status").val(), version: id ? Number($("#order-version").val()) : null };
        isSaving = true;
        setBusy("#order-modal", true);
        $("#form-message").addClass("d-none");

        try {
            await sendRequest(id ? `/api/pedidos/${id}` : "/api/pedidos", id ? "PUT" : "POST", input);
            isSaving = false;
            orderModal.hide();
            showMessage("#page-message", id ? "Pedido atualizado com sucesso." : "Pedido criado com sucesso.", "success");
            table.ajax.reload(null, false);
        } catch (problem) {
            showFormError(problem);
        } finally {
            isSaving = false;
            setBusy("#order-modal", false);
        }
    });

    $("#orders-table").on("click", ".delete", function () {
        selectedOrder = table.row($(this).closest("tr")).data();
        $("#delete-description").text(`O pedido #${selectedOrder.id} de ${selectedOrder.customerName} será excluído.`);
        $("#delete-message").addClass("d-none").empty();
        deleteModal.show();
    });

    $("#confirm-delete").on("click", async function () {
        if (isSaving || !selectedOrder) {
            return;
        }

        isSaving = true;
        setBusy("#delete-modal", true);

        try {
            await sendRequest(`/api/pedidos/${selectedOrder.id}?version=${selectedOrder.version}`, "DELETE");
            isSaving = false;
            deleteModal.hide();
            showMessage("#page-message", "Pedido excluído com sucesso.", "success");
            table.ajax.reload(null, false);
        } catch (problem) {
            showMessage("#delete-message", problem.detail || problem.title);
            table.ajax.reload(null, false);
        } finally {
            isSaving = false;
            setBusy("#delete-modal", false);
        }
    });

    let searchTimeout;
    $("#order-search").on("input", function () {
        clearTimeout(searchTimeout);
        const search = this.value;
        searchTimeout = setTimeout(() => table.search(search).draw(), 350);
    });

    $("#refresh-orders").on("click", function () { table.ajax.reload(null, false); });
    const refreshInterval = setInterval(function () { if (!activeTableRequest && !isSaving && !document.hidden) { table.ajax.reload(null, false); } }, 10_000);
    window.addEventListener("pagehide", function () { clearInterval(refreshInterval); if (activeTableRequest) { activeTableRequest.abort(); } });
});
