(function ($) {
    'use strict';

    $(function () {
        var form = $('#employeeCreateForm');
        if (!form.length) return;

        var gridNode = $('#employeeCreateGrid');
        var rows = JSON.parse($('#employeeCreateGridData').text() || '[]');
        var templates = {};
        var originalValues = {};
        var drafts = {};
        var submitting = false;

        function showMessage(text, kind) {
            if (!text) window.TIRMessages.clear();
            else window.TIRMessages.show(text, kind);
        }

        function readValue(node) {
            var root = $(node);
            var input = root.find('[name="Field.RawValue"]');
            return {
                RawValue: input.length ? String(input.val() ?? '') : '',
                BooleanIsSet: root.find('[name="Field.BooleanIsSet"]').prop('checked') === true,
                BooleanChecked: root.find('[name="Field.BooleanChecked"]').prop('checked') === true
            };
        }

        function applyValue(node, value) {
            var root = $(node);
            root.find('[name="Field.RawValue"]').val(value.RawValue);
            root.find('[name="Field.BooleanIsSet"]').prop('checked', value.BooleanIsSet);
            root.find('[name="Field.BooleanChecked"]').prop('checked', value.BooleanChecked);
        }

        $('#employeeCreateFieldTemplates .employee-detail-template').each(function () {
            var id = Number($(this).attr('data-attribute-id'));
            templates[id] = $(this);
            originalValues[id] = readValue(this);
        });

        window.TIRDataGrid.createEmployeeFieldGrid(gridNode, rows, function (container, row) {
            var id = row.AttributeId;
            var wrapper = $('<div class="employee-detail-editor">')
                .attr('data-attribute-id', id).appendTo(container);
            if (!templates[id]) return;
            wrapper.append(templates[id].children().clone());
            if (drafts[id]) applyValue(wrapper, drafts[id]);
        });

        gridNode.on('input change', '.employee-detail-editor :input', function () {
            var root = $(this).closest('.employee-detail-editor');
            drafts[Number(root.attr('data-attribute-id'))] = readValue(root);
        });

        form.on('submit', function (event) {
            event.preventDefault();
            if (submitting || !form[0].reportValidity()) return;

            gridNode.find('.employee-detail-editor').each(function () {
                var id = Number($(this).attr('data-attribute-id'));
                drafts[id] = readValue(this);
            });

            var payload = $('#employeeCreatePayload').empty();
            function appendValue(name, value) {
                $('<input type="hidden">').attr('name', name).val(value).appendTo(payload);
            }

            var fieldIndex = 0;
            rows.forEach(function (row) {
                if (row.SourceKind !== 1) return;
                var value = drafts[row.AttributeId] || originalValues[row.AttributeId];
                if (!value) return;
                var prefix = 'Fields[' + fieldIndex++ + '].';
                appendValue(prefix + 'AttributeId', row.AttributeId);
                appendValue(prefix + 'RawValue', value.RawValue);
                appendValue(prefix + 'BooleanIsSet', value.BooleanIsSet ? 'true' : 'false');
                appendValue(prefix + 'BooleanChecked', value.BooleanChecked ? 'true' : 'false');
            });

            gridNode.find(':input').prop('disabled', true);
            submitting = true;
            $('#employeeCreateSubmit').prop('disabled', true).text('در حال افزودن...');
            showMessage('', 'info');
            if (window.TIRLoading) window.TIRLoading.show('در حال افزودن کارمند...');
            $.ajax({
                url: form.attr('action') || window.location.href,
                type: 'POST', data: form.serialize(), dataType: 'html'
            }).done(function (markup) {
                if (markup.indexOf('tir:popup-complete') >= 0) {
                    if (window.parent !== window) {
                        window.parent.postMessage({ type: 'tir:popup-complete', scope: 'employees' },
                            window.location.origin);
                    } else showMessage('کارمند با موفقیت افزوده شد.', 'success');
                    return;
                }
                var documentResponse = new DOMParser().parseFromString(markup, 'text/html');
                var errors = Array.from(documentResponse.querySelectorAll(
                    '.validation-summary-errors li, .field-validation-error'))
                    .map(function (node) { return node.textContent.trim(); }).filter(Boolean);
                showMessage(errors.length ? Array.from(new Set(errors)).join(' ـ ') :
                    'ثبت کارمند انجام نشد. اطلاعات فرم را بررسی کنید.', 'danger');
            }).fail(function (xhr) {
                var response = xhr.responseJSON || {};
                showMessage(response.message || 'خطا در افزودن کارمند؛ لطفاً دوباره تلاش کنید.', 'danger');
            }).always(function () {
                submitting = false;
                gridNode.find(':input').prop('disabled', false);
                $('#employeeCreateSubmit').prop('disabled', false).text('افزودن');
                if (window.TIRLoading) window.TIRLoading.hide();
            });
        });
    });
})(jQuery);
