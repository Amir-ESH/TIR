(function ($) {
    'use strict';

    $(function () {
        var screen = $('#employeeDetailsScreen');
        if (!screen.length) return;

        var form = $('#employeeDetailsFixedForm');
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

        // بازخوانی بی صدای اطلاعات جدید، به جای رفرش صفحه و از دست دادن وضعیت گرید.
        function refreshFields() {
            return $.get(window.location.href).then(function (markup) {
                var documentResponse = new DOMParser().parseFromString(markup, 'text/html');
                var nextTemplates = documentResponse.querySelector('#employeeDetailsFieldTemplates');
                var nextGridData = documentResponse.querySelector('#employeeDetailsGridData');
                if (!nextTemplates || !nextGridData) throw new Error('پاسخ بازخوانی ناقص است.');
                $('#employeeDetailsFieldTemplates').html(nextTemplates.innerHTML);
                templates = {};
                originals = {};
                drafts = {};
                $('#employeeDetailsFieldTemplates .employee-detail-template').each(function () {
                    var id = Number($(this).attr('data-attribute-id'));
                    templates[id] = $(this);
                    originals[id] = readField(this);
                });
                rows = JSON.parse(nextGridData.textContent || '[]');
                gridNode.dxDataGrid('instance').option('dataSource', rows);
                var nextMeta = documentResponse.querySelector('.employee-details-meta');
                if (nextMeta) $('.employee-details-meta').html(nextMeta.innerHTML);
                var nextTitle = documentResponse.querySelector('#employeeDetailsScreen .section-title');
                if (nextTitle) screen.find('.employee-details-toolbar .section-title').text(nextTitle.textContent);
            });
        }

        window.TIRDataGrid.createEmployeeFieldGrid(gridNode, rows, function (container, row) {
            var id = row.AttributeId;
            var wrapper = $('<div class="employee-detail-editor">')
                .attr('data-attribute-id', id).appendTo(container);
            if (!templates[id]) return;
            wrapper.append(templates[id].children().clone());
            if (drafts[id]) applyField(wrapper, drafts[id]);
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
            window.TIRMessages.show(value, kind, { errors: errors });
        }

        function setBusy(value) {
            busy = value;
            saveButtons.prop('disabled', value).text(value ? 'در حال ذخیره...' : 'ذخیره');
            $('#employeeDetailsCalculate').prop('disabled', value);
            if (window.TIRLoading) {
                if (value) window.TIRLoading.show('در حال پردازش اطلاعات کارمند...');
                else window.TIRLoading.hide();
            }
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
            window.TIRMessages.clear();
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
                    refreshFields().done(function () {
                        showMessage('اطلاعات اصلی و مقادیر مشخصه ها با موفقیت ذخیره شدند.', 'success');
                    }).fail(function () {
                        showMessage('اطلاعات ذخیره شد، اما بازخوانی مقادیر جدید انجام نشد.', 'warning');
                    }).always(function () { setBusy(false); });
                }).fail(function (response) {
                    var result = response.responseJSON || {};
                    showMessage('ذخیره مشخصه ها موفق نبود. اطلاعات اصلی در صورت تغییر ذخیره شده اند. ' +
                        (result.message || 'خطا در ذخیره مقادیر.'), 'danger', result.errors);
                    setBusy(false);
                });
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
                if (answer.success) {
                    refreshFields().done(function () {
                        showMessage(answer.message || 'فرمول ها محاسبه و مقادیر به روز شدند.', 'success');
                    }).fail(function () {
                        showMessage('محاسبه انجام شد اما نمایش نتیجه جدید امکان پذیر نبود.', 'warning');
                    }).always(function () { setBusy(false); });
                } else {
                    showMessage(answer.message || 'محاسبه انجام نشد.', 'danger');
                    setBusy(false);
                }
            }).fail(function (response) {
                var answer = response.responseJSON || {};
                showMessage(answer.message || 'خطا در محاسبه فرمول ها.', 'danger');
                setBusy(false);
            });
        });
    });
})(jQuery);
