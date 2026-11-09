(function ($) {
    'use strict';

    var types = {
        success: { css: 'success', icon: '✓', title: 'موفقیت' },
        danger: { css: 'danger', icon: '!', title: 'خطا' },
        warning: { css: 'warning', icon: '!', title: 'هشدار' },
        info: { css: 'info', icon: 'i', title: 'اطلاع' }
    };

    function kindOf(kind) {
        var name = String(kind || 'info').toLowerCase();
        if (name === 'error') name = 'danger';
        return types[name] ? name : 'info';
    }

    function hostFor(options) {
        var container = options && options.container ? $(options.container).first() : $();
        if (container.length) {
            var local = container.children('.tir-messages-host').first();
            if (!local.length)
                local = $('<div class="tir-messages-host tir-messages-in-modal" dir="rtl">')
                    .appendTo(container);
            return local;
        }
        var global = $('#tirMessagesHost');
        if (!global.length)
            global = $('<div id="tirMessagesHost" class="tir-messages-host" dir="rtl">')
                .appendTo('body');
        return global;
    }

    function clear(options) {
        hostFor(options).empty();
    }

    function show(message, kind, options) {
        if (message === null || message === undefined || String(message).trim() === '')
            return null;
        var settings = options || {};
        var selected = types[kindOf(kind)];
        var host = hostFor(settings);
        var notice = $('<div class="tir-message">')
            .addClass('tir-message-' + selected.css)
            .attr('role', selected.css === 'danger' ? 'alert' : 'status')
            .attr('aria-label', selected.title);
        var body = $('<div class="tir-message-body">').appendTo(notice);
        $('<div class="tir-message-text">').text(message).appendTo(body);
        if (Array.isArray(settings.errors) && settings.errors.length) {
            var list = $('<ul class="tir-message-errors">').appendTo(body);
            settings.errors.forEach(function (error) { $('<li>').text(error).appendTo(list); });
        }
        $('<span class="tir-message-icon" aria-hidden="true">')
            .text(selected.icon).appendTo(notice);
        $('<button type="button" class="tir-message-close" aria-label="بستن پیام" title="بستن پیام">')
            .text('×').on('click', function () { notice.remove(); }).appendTo(notice);
        host.append(notice);
        while (host.children().length > 4) host.children().first().remove();

        var lifetime = settings.duration === undefined
            ? (selected.css === 'danger' ? 0 : selected.css === 'warning' ? 8500 : 5000)
            : Number(settings.duration);
        if (lifetime > 0) window.setTimeout(function () { notice.remove(); }, lifetime);
        return notice;
    }

    function readServerMessages(root) {
        $(root || document).find('[data-tir-flash]').each(function () {
            var node = $(this);
            show(node.text().trim(), node.attr('data-tir-flash'));
            node.remove();
        });
    }

    window.TIRMessages = {
        show: show, clear: clear, readServerMessages: readServerMessages,
        success: function (value, options) { return show(value, 'success', options); },
        error: function (value, options) { return show(value, 'danger', options); },
        warning: function (value, options) { return show(value, 'warning', options); },
        info: function (value, options) { return show(value, 'info', options); }
    };
    $(function () { readServerMessages(document); });
})(jQuery);
