(function ($) {
    'use strict';

    $(function () {
        var screen = $('#employeeDetailsScreen');
        if (!screen.length) return;

        var form = $('#employeeDetailsFixedForm');
        var message = $('#employeeDetailsMessage');
        var saveButtons = $('.js-employee-save');
        var gridNode = $('#employeeDetailsGrid');
        var rows = JSON.parse($('#employeeDetailsGridData').text() || '[]');
        var templates = {};
        var originals = {};
        var drafts = {};
        var busy = false;

        function readField(node) {
            var root = $(node);
            var valueInput = root.find('[name="Field.RawValue"]');
            return {
                AttributeId: Number(root.find('[name="Field.AttributeId"]').val()),
                ExpectedRowVersion: root.find('[name="Field.ExpectedRowVersion"]').val() || null,
                RawValue: valueInput.length ? String(valueInput.val() ?? '') : null,
                BooleanIsSet: root.find('[name="Field.BooleanIsSet"]').prop('checked') === true,
                BooleanChecked: root.find('[name="Field.BooleanChecked"]').prop('checked') === true,
                ClearValue: false
            };
        }

        function applyField(node, value) {
            var root = $(node);
            root.find('[name="Field.RawValue"]').val(value.RawValue);
            root.find('[name="Field.BooleanIsSet"]').prop('checked', value.BooleanIsSet);
            root.find('[name="Field.BooleanChecked"]').prop('checked', value.BooleanChecked);
        }

        function comparable(value) {
            return JSON.stringify([value.RawValue, value.BooleanIsSet, value.BooleanChecked]);
        }

        $('#employeeDetailsFieldTemplates .employee-detail-template').each(function () {
            var field = $(this);
            var id = Number(field.attr('data-attribute-id'));
            templates[id] = field;
            originals[id] = readField(field);
        });

        window.TIRDataGrid.create(gridNode, {
            keyExpr: 'AttributeId',
            dataSource: rows,
            // حداکثر حدود شش ردیف دیده می شود و بقیه داخل خود گرید اسکرول می شوند.
            height: Math.min(650, Math.max(440, window.innerHeight - 205)),
            columnAutoWidth: false,
            wordWrapEnabled: true,
            columns: [
                { dataField: 'Code', caption: 'کد مشخصه', width: 175 },
                { dataField: 'Name', caption: 'عنوان فارسی', minWidth: 200 },
                { dataField: 'TypeTitle', caption: 'نوع فیلد', width: 145 },
                { caption: 'مقدار', minWidth: 300, allowSorting: false,
                    allowFiltering: false, allowGrouping: false, allowSearch: false,
                    cellTemplate: function (container, options) {
                        var id = options.data.AttributeId;
                        var wrapper = $('<div class="employee-detail-editor">')
                            .attr('data-attribute-id', id).appendTo(container);
                        if (!templates[id]) return;
                        wrapper.append(templates[id].children().clone());
                        if (drafts[id]) applyField(wrapper, drafts[id]);
                    } }
            ],
            paging: { enabled: false }, pager: { visible: false },
            sorting: { mode: 'multiple' },
            filterRow: { visible: true, applyFilter: 'auto' },
            headerFilter: { visible: true },
            searchPanel: { visible: true, width: 250, highlightSearchText: true },
            groupPanel: { visible: true },
            columnChooser: { enabled: true, mode: 'select' },
            allowColumnReordering: true,
            scrolling: { mode: 'standard', showScrollbar: 'always', useNative: true },
            noDataText: 'مشخصه ای برای این کارمند وجود ندارد.'
        });

        gridNode.on('input change', '.employee-detail-editor :input', function () {
            var root = $(this).closest('.employee-detail-editor');
            var id = Number(root.attr('data-attribute-id'));
            drafts[id] = readField(root);
        });

        var initialFixed = form.serialize();
        function changedFields() {
            return Object.keys(drafts).map(function (id) { return drafts[id]; })
                .filter(function (value) {
                    return originals[value.AttributeId] &&
                        comparable(value) !== comparable(originals[value.AttributeId]);
                });
        }

        function showMessage(value, kind, errors) {
            var box = $('<div class="alert">').addClass('alert-' + kind).text(value);
            if (Array.isArray(errors) && errors.length) {
                var list = $('<ul class="mt-2 mb-0">').appendTo(box);
                errors.forEach(function (error) { $('<li>').text(error).appendTo(list); });
            }
            message.empty().append(box);
            message[0].scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        }

        function setBusy(value) {
            busy = value;
            saveButtons.prop('disabled', value).text(value ? 'در حال ذخیره...' : 'ذخیره');
            $('#employeeDetailsCalculate').prop('disabled', value);
        }

        function notifyParent() {
            if (window.parent === window) return;
            window.parent.postMessage({
                type: 'tir:employee-details-saved',
                id: Number(screen.attr('data-employee-id')),
                employeeCode: form.find('[name="Input.EmployeeCode"]').val(),
                firstName: form.find('[name="Input.FirstName"]').val(),
                lastName: form.find('[name="Input.LastName"]').val()
            }, window.location.origin);
        }

        function saveAll() {
            if (busy || !form.length || !form[0].reportValidity()) return;
            var modifiedValues = changedFields();
            if (form.serialize() === initialFixed && !modifiedValues.length) {
                showMessage('تغییری برای ذخیره وجود ندارد.', 'info');
                return;
            }

            var token = form.find('[name="__RequestVerificationToken"]').val();
            if (!token) return showMessage('اطلاعات امنیتی فرم پیدا نشد. صفحه را تازه سازی کنید.', 'danger');

            setBusy(true);
            message.empty();
            $.ajax({
                url: form.attr('action'), type: 'POST', data: form.serialize(),
                headers: { RequestVerificationToken: token }
            }).done(function (fixed) {
                if (!fixed.success) {
                    showMessage(fixed.message || 'ذخیره اطلاعات اصلی انجام نشد.', 'danger');
                    setBusy(false);
                    return;
                }
                form.find('[name="Input.ExpectedRowVersion"]').val(fixed.rowVersion);
                initialFixed = form.serialize();
                if (fixed.changed) notifyParent();

                if (!modifiedValues.length) {
                    showMessage('اطلاعات اصلی ذخیره شد.', 'success');
                    setBusy(false);
                    if (fixed.changed) window.setTimeout(function () { window.location.reload(); }, 650);
                    return;
                }

                $.ajax({
                    url: screen.attr('data-values-url'), type: 'POST',
                    contentType: 'application/json; charset=utf-8',
                    headers: { RequestVerificationToken: token },
                    data: JSON.stringify(modifiedValues)
                }).done(function (result) {
                    if (!result.success) {
                        showMessage(result.message || 'ذخیره مشخصه ها انجام نشد.', 'danger', result.errors);
                        return;
                    }
                    notifyParent();
                    showMessage('اطلاعات اصلی و مقادیر مشخصه ها با موفقیت ذخیره شدند.', 'success');
                    window.setTimeout(function () { window.location.reload(); }, 700);
                }).fail(function (response) {
                    var result = response.responseJSON || {};
                    showMessage('ذخیره مشخصه ها موفق نبود. اطلاعات اصلی در صورت تغییر ذخیره شده اند. ' +
                        (result.message || 'خطا در ذخیره مقادیر.'), 'danger', result.errors);
                }).always(function () { setBusy(false); });
            }).fail(function (response) {
                var result = response.responseJSON || {};
                showMessage(result.message || 'خطا در ذخیره اطلاعات اصلی کارمند.', 'danger');
                setBusy(false);
            });
        }

        saveButtons.on('click', saveAll);
        form.on('submit', function (event) { event.preventDefault(); saveAll(); });

        $('#employeeDetailsCalculate').on('click', function () {
            if (busy) return;
            if (form.serialize() !== initialFixed || changedFields().length) {
                showMessage('ابتدا تغییرات را ذخیره کنید، سپس فرمول ها را محاسبه کنید.', 'warning');
                return;
            }
            if (!window.confirm('فرمول های این کارمند دوباره محاسبه شوند؟')) return;
            setBusy(true);
            $.ajax({
                url: screen.attr('data-calculate-url'), type: 'POST',
                headers: { RequestVerificationToken: form.find('[name="__RequestVerificationToken"]').val() }
            }).done(function (answer) {
                if (answer.success) window.location.reload();
                else showMessage(answer.message || 'محاسبه انجام نشد.', 'danger');
            }).fail(function (response) {
                var answer = response.responseJSON || {};
                showMessage(answer.message || 'خطا در محاسبه فرمول ها.', 'danger');
            }).always(function () { setBusy(false); });
        });
    });
})(jQuery);
