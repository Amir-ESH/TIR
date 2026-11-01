(function ($) {
    'use strict';
    $(function () {
        var attributes = JSON.parse($('#attributeData').text() || '[]');
        var operators = [
            ['eq', 'مساوی'], ['ne', 'نامساوی'], ['gt', 'بزرگ تر'], ['gte', 'بزرگ تر یا مساوی'],
            ['lt', 'کوچک تر'], ['lte', 'کوچک تر یا مساوی'], ['between', 'بین'], ['contains', 'شامل متن']
        ];
        var container = $('#conditionsContainer');
        var optionCache = {};
        var optionsUrl = $('#attributeData').data('options-url');

        function createRow(condition) {
            if (container.children().length >= 64) { alert('حداکثر ۶۴ شرط مجاز است.'); return; }
            var row = $('<div class="query-row"></div>');
            $('<input type="number" min="1" max="16" class="form-control condition-group" title="شماره گروه OR" />')
                .val(condition.group || 1).appendTo(row);
            var attribute = $('<select class="form-select condition-attribute"></select>').appendTo(row);
            $('<option value="">انتخاب مشخصه</option>').appendTo(attribute);
            attributes.forEach(function (item) {
                $('<option></option>').val(item.id).text(item.title).attr('data-kind', item.kind).appendTo(attribute);
            });
            var operator = $('<select class="form-select condition-operator"></select>').appendTo(row);
            operators.forEach(function (item) { $('<option></option>').val(item[0]).text(item[1]).appendTo(operator); });
            $('<input class="form-control condition-value" placeholder="مقدار" />').val(condition.value || '').appendTo(row);
            $('<input class="form-control to-value condition-to" placeholder="تا مقدار" />').val(condition.to || '').appendTo(row);
            $('<button type="button" class="btn btn-outline-danger js-remove-condition" title="حذف شرط">×</button>').appendTo(row);
            container.append(row);
            if (condition.attributeId) attribute.val(condition.attributeId.toString());
            if (condition.operator) operator.val(condition.operator);
            updateRow(row);
        }

        function updateRow(row) {
            var kind = Number(row.find('.condition-attribute option:selected').data('kind'));
            var selectedAttribute = Number(row.find('.condition-attribute').val());
            var operator = row.find('.condition-operator');
            operator.find('option').each(function () {
                var code = $(this).val();
                var allow = code === 'eq' || code === 'ne' ||
                    (code === 'contains' && kind === 1) ||
                    (['gt', 'gte', 'lt', 'lte', 'between'].includes(code) && [2, 3, 4, 5, 6].includes(kind));
                $(this).prop('disabled', !allow);
            });
            if (operator.find('option:selected').prop('disabled')) operator.val('eq');
            row.toggleClass('is-between', operator.val() === 'between');
            var valueField = row.find('.condition-value');
            if (kind === 8 && !valueField.is('select')) {
                var priorValue = valueField.val();
                valueField.replaceWith($('<select class="form-select condition-value"><option value="">در حال بارگذاری...</option></select>'));
                valueField = row.find('.condition-value');
                valueField.data('prior', priorValue);
            } else if (kind !== 8 && valueField.is('select')) {
                valueField.replaceWith($('<input class="form-control condition-value" placeholder="مقدار" />'));
                valueField = row.find('.condition-value');
            }
            if (kind === 8 && selectedAttribute) {
                var wanted = valueField.data('prior') || valueField.val();
                var fillOptions = function (options) {
                    if (Number(row.find('.condition-attribute').val()) !== selectedAttribute) return;
                    var select = row.find('.condition-value').empty();
                    $('<option value="">انتخاب گزینه</option>').appendTo(select);
                    options.forEach(function (item) {
                        $('<option></option>').val(item.id).text(item.title).appendTo(select);
                    });
                    select.val(wanted);
                    select.removeData('prior');
                };
                if (optionCache[selectedAttribute]) fillOptions(optionCache[selectedAttribute]);
                else $.getJSON(optionsUrl, { attributeId: selectedAttribute })
                    .done(function (options) { optionCache[selectedAttribute] = options; fillOptions(options); })
                    .fail(function () { row.find('.condition-value').empty().append('<option value="">بارگذاری گزینه ها ناموفق بود</option>'); });
            }
            row.find('.condition-value,.condition-to').removeClass('js-jalali-date js-jalali-datetime');
            if (kind === 4) row.find('.condition-value,.condition-to').addClass('js-jalali-date');
            if (kind === 6) row.find('.condition-value,.condition-to').addClass('js-jalali-datetime');
            var placeholder = kind === 4 ? '۱۴۰۵/۰۸/۰۹' : kind === 6 ? '۱۴۰۵/۰۸/۰۹ ۱۲:۳۰' :
                kind === 7 ? '0 یا 1' : kind === 8 ? 'شناسه گزینه' : 'مقدار';
            row.find('.condition-value').attr('placeholder', placeholder);
        }

        $('#addCondition').on('click', function () { createRow({ group: 1 }); });
        container.on('click', '.js-remove-condition', function () { $(this).closest('.query-row').remove(); });
        container.on('change', '.condition-operator,.condition-attribute', function () { updateRow($(this).closest('.query-row')); });

        $('#advancedSearchForm').on('submit', function () {
            var conditions = [];
            container.children('.query-row').each(function () {
                var row = $(this), attr = Number(row.find('.condition-attribute').val());
                if (!attr) return;
                conditions.push({ group: Number(row.find('.condition-group').val()),
                    attributeId: attr, operator: row.find('.condition-operator').val(),
                    value: String(row.find('.condition-value').val() || '').trim(),
                    to: String(row.find('.condition-to').val() || '').trim() || null });
            });
            $('#FiltersJson').val(JSON.stringify(conditions));
        });

        $('.js-search-page').on('click', function () {
            $('#PageNumber').val($(this).data('page'));
            $('#advancedSearchForm').trigger('submit');
        });

        var previous = [];
        try { previous = JSON.parse($('#FiltersJson').val() || '[]'); } catch (_) { previous = []; }
        if (previous.length) previous.forEach(createRow);
        else createRow({ group: 1 });
    });
})(jQuery);
