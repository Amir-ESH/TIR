(function ($) {
    'use strict';

    var overlay = null;
    var outstanding = 0;

    function showLoading(message) {
        outstanding++;
        if (!overlay) {
            overlay = $('<div class="busy-overlay" role="status" aria-live="polite" aria-busy="true">' +
                '<div class="busy-content"><div class="spinner-border text-primary mb-3" aria-hidden="true"></div>' +
                '<p class="mb-0 js-loading-text"></p></div></div>').appendTo('body');
        }
        overlay.find('.js-loading-text').text(message || 'در حال انجام عملیات...');
        overlay.addClass('show');
    }

    function hideLoading() {
        outstanding = Math.max(0, outstanding - 1);
        if (!outstanding && overlay) overlay.removeClass('show');
    }

    window.TIRLoading = { show: showLoading, hide: hideLoading };

    function messageBox(message, kind) {
        window.TIRMessages.show(message, kind);
    }

    $(document).on('click', 'form[data-tir-ajax-form] [type="submit"]', function () {
        $(this).closest('form').data('tirSubmitter', this);
    });

    $(document).on('submit', 'form[data-tir-ajax-form]', function (event) {
        if (event.isDefaultPrevented()) return;
        event.preventDefault();
        var form = $(this);
        if (form.data('tirBusy') || !this.reportValidity()) return;
        var clicked = event.originalEvent && event.originalEvent.submitter || form.data('tirSubmitter');
        var button = clicked ? $(clicked) : form.find('[type="submit"]:focus');
        if (!button.length) button = form.find('[type="submit"]').first();
        var values = form.serializeArray();
        if (button.attr('name')) values.push({ name: button.attr('name'), value: button.val() });
        form.data('tirBusy', true);
        form.find('[type="submit"]').prop('disabled', true);
        showLoading('در حال ذخیره اطلاعات...');
        $.ajax({
            url: form.attr('action') || window.location.href,
            type: 'POST', data: $.param(values), dataType: 'html'
        }).done(function (markup) {
            if (markup.indexOf('tir:popup-complete') >= 0) {
                if (window.parent !== window) {
                    window.parent.postMessage({ type: 'tir:popup-complete', scope: 'attributes' },
                        window.location.origin);
                } else {
                    messageBox('عملیات با موفقیت انجام شد.', 'success');
                    form.find('[type="submit"]').prop('disabled', true);
                }
                return;
            }
            var page = new DOMParser().parseFromString(markup, 'text/html');
            var nextMain = page.querySelector('#main-content');
            if (!nextMain) {
                messageBox('پاسخ سرور قابل نمایش نیست؛ لطفاً وضعیت عملیات را بررسی کنید.', 'danger');
                return;
            }
            var nodes = $(nextMain).children().filter(function () { return this.tagName !== 'SCRIPT'; });
            $('#main-content').empty().append(nodes);
            window.TIRMessages.readServerMessages($('#main-content'));
            if (markup.indexOf('tir:popup-changed') >= 0 && window.parent !== window) {
                window.parent.postMessage({ type: 'tir:popup-changed', scope: 'attributes' },
                    window.location.origin);
            }
        }).fail(function (xhr) {
            var response = xhr.responseJSON || {};
            messageBox(response.message || response.error || 'عملیات انجام نشد. دوباره تلاش کنید.', 'danger');
        }).always(function () {
            form.data('tirBusy', false);
            form.find('[type="submit"]').prop('disabled', false);
            hideLoading();
        });
    });
})(jQuery);
