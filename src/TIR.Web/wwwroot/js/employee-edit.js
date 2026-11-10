(function ($) {
    'use strict';

    $(function () {
        var container = $('#employeeDynamicValues');
        if (!container.length) return;

        var forms = container.find('.dynamic-value-form');
        var buttons = container.find('.js-save-all-values');

        function fingerprint(form) {
            return $(form).find('[name="Field.RawValue"],[name="Field.BooleanIsSet"],[name="Field.BooleanChecked"]')
                .serialize();
        }

        forms.each(function () {
            $(this).data('initial-value', fingerprint(this));
        });

        forms.on('tir:field-saved', function () {
            $(this).data('initial-value', fingerprint(this));
        });

        function showMessage(text, kind, errors) {
            window.TIRMessages.show(text, kind, { errors: errors });
        }

        buttons.on('click', function () {
            var changed = forms.filter(function () {
                var form = $(this);
                var editable = form.find('[name="Field.RawValue"],[name="Field.BooleanIsSet"],[name="Field.BooleanChecked"]').length > 0;
                return editable && fingerprint(this) !== form.data('initial-value');
            });

            if (!changed.length) {
                showMessage('هیچ مشخصه ای تغییر نکرده است.', 'info');
                return;
            }

            var token = forms.first().find('input[name="__RequestVerificationToken"]').val();
            if (!token) {
                showMessage('اطلاعات امنیتی فرم پیدا نشد. صفحه را دوباره باز کنید.', 'danger');
                return;
            }

            var values = [];
            changed.each(function () {
                var value = {};

                $(this).serializeArray().forEach(function (entry) {
                    if (entry.name.startsWith('Field.')) {
                        value[entry.name.substring(6)] = entry.value;
                    }
                });

                value.AttributeId = Number(value.AttributeId);
                value.BooleanIsSet = value.BooleanIsSet === 'true';
                value.BooleanChecked = value.BooleanChecked === 'true';
                value.ClearValue = value.ClearValue === 'true';
                values.push(value);
            });

            buttons.prop('disabled', true).text('در حال ذخیره...');
            forms.find(':input').prop('disabled', true);

            $.ajax({
                url: container.attr('data-save-all-url'),
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                headers: { RequestVerificationToken: token },
                data: JSON.stringify(values),
                success: function (answer) {
                    if (!answer.success) {
                        showMessage(answer.message || 'ذخیره مشخصه ها انجام نشد.', 'danger', answer.errors);
                        return;
                    }

                    window.location.reload();
                },
                error: function (response) {
                    var answer = response.responseJSON || {};
                    showMessage(answer.message || 'خطا در ذخیره مشخصه ها رخ داد.', 'danger', answer.errors);
                },
                complete: function () {
                    forms.find(':input').prop('disabled', false);
                    buttons.prop('disabled', false).text('ذخیره همه مشخصه ها');
                }
            });
        });
    });
})(jQuery);
