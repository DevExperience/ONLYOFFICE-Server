/*
(c) Copyright Ascensio System SIA 2010-2014

This program is a free software product.
You can redistribute it and/or modify it under the terms 
of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of 
any third-party rights.

This program is distributed WITHOUT ANY WARRANTY; without even the implied warranty 
of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see 
the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html

You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.

The  interactive user interfaces in modified source and object code versions of the Program must 
display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
 
Pursuant to Section 7(b) of the License you must retain the original Product logo when 
distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under 
trademark law for use of our trademarks.
 
All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode
*/

/*
    Copyright (c) Ascensio System SIA 2013. All rights reserved.
    http://www.teamlab.com
*/
(function ($, win, doc, body) {
  function converText (str) {
    var o = document.createElement('textarea');
    o.innerHTML = str;
    return o.value;
  }

  function resetDefaultValue ($select) {
    var selectvalues = $select.val(),
        selectvaluesInd = 0,
        selectvalue = '';

    selectvalues = typeof selectvalues === 'string' ? [selectvalues] : selectvalues || [];
    selectvalue = [].concat([''], selectvalues, ['']).join('xyu');

    if (selectvalue.indexOf('xyu-1xyu') !== -1) {
      selectvaluesInd = selectvalues.length;
      while (selectvaluesInd--) {
        if (selectvalues[selectvaluesInd] == -1) {
          selectvalues.splice(selectvaluesInd, 1);
          break;
        }
      }
      $select.val(selectvalues);
    }
  }

  function onBodyClick (evt) {
    var $target = evt ? jQuery(evt.target) : $();
    if ($target.is('select')) {
      jQuery(document.body).unbind('click', arguments.callee);
      jQuery(document.body).one('click', arguments.callee);

      return undefined;
    }

    jQuery('span.custom-combobox').removeClass('showed-options').find('div.combobox-container:first').hide();
  }

  function onSelectFocus (evt) {
    onComboboxTitleClick(evt);
  }

  function onSelectBlur (evt) {
    onBodyClick();
    jQuery(document.body).unbind('click', onBodyClick);
  }

  function onSelectChange (evt) {
    var
      $this = jQuery(this),
      $combobox = $this.parents('span.custom-combobox:first'),
      $option = null,
      value = '',
      defaultoption = null,
      selectvalues = $this.val(),
      selectvalue = '',
      optionstitle = [],
      optionclassname = [],
      selectoption = null,
      option = null,
      options = $this.find('option'),
      $options = $combobox.find('li.option-item'),
      $optionsInd = 0,
      optionsInd = 0,
      isselected = false;

    selectvalues = typeof selectvalues === 'string' ? [selectvalues] : selectvalues || [];
    selectvalue = [].concat([''], selectvalues, ['']).join('xyu');

    optionsInd = options.length;
    while (optionsInd--) {
      option = options[optionsInd];
      value = option.getAttribute('value');
      if (value == -1) {
        defaultoption = option;
        continue;
      }
      isselected = false;
      if (selectvalue.indexOf('xyu' + value + 'xyu') !== -1) {
        isselected = true;
        selectoption = option;
        optionstitle.unshift(selectoption.innerHTML);
        optionclassname.unshift(selectoption.className);
      }
      $optionsInd = $options.length;
      while ($optionsInd--) {
        if ($options[$optionsInd].getAttribute('data-value') === value) {
          jQuery($options[$optionsInd]).find('input').attr('checked', isselected === true);
          break;
        }
      }
      //$combobox.find('li.option-item[data-value="' + value + '"]:first').find('input').attr('checked', isselected === true);
    }

    if (defaultoption && optionstitle.length === 0) {
      optionstitle.unshift(defaultoption.innerHTML);
    }

    var
      classname = '',
      classes = $combobox.attr('class').split(/\s+/),
      classesInd = 0;
    classesInd = classes ? classes.length : 0;
    while (classesInd--) {
      classname = classes[classesInd];
      if (classname.indexOf('select-value-') === 0) {
        $combobox.removeClass(classname);
      }
    }

    $combobox
      .removeClass($combobox.attr('data-optclass') || ' ')
      .find('span.combobox-title-inner-text:first').text(converText(optionstitle.length === 1 ? optionstitle[0] : optionstitle.join(', ')));

    $combobox.attr('data-value', value).attr('data-optclass', optionclassname.join(' ')).addClass(optionclassname.join(' ')).addClass('select-value-' + value);
      //.find('li.option-item[data-value="' + value + '"]:first').addClass('selected-item').siblings().removeClass('selected-item');

    $optionsInd = $options.length;
      while ($optionsInd--) {
        if ($options[$optionsInd].getAttribute('data-value') === value) {
          jQuery($options[$optionsInd]).addClass('selected-item').siblings().removeClass('selected-item');
          break;
        }
      }
  }

  function onComboboxTitleClick (evt) {
    var $combobox = $(evt.target).parents('span.custom-combobox:first');
    if ($combobox.hasClass('showed-options')) {
      return undefined;
    }

    onBodyClick();
    jQuery(document.body).unbind('click', onBodyClick);
    resetDefaultValue($combobox.find('select'));
    $combobox.addClass('showed-options').find('div.combobox-container:first').show();
    setTimeout(setBodyEvents, 1);
  }

  function onComboboxOptionClick (evt) {
    var
      $combobox = jQuery(this).parents('span.custom-combobox:first'),
      $select = $combobox.find('select:first'),
      $option = jQuery(evt.target),
      value = $option.attr('data-value');

    if (value) {
      $select.val(value).change();
    }
  }

  function onComboboxCheckboxClick (evt) {
    var
      $combobox = jQuery(this).parents('span.custom-combobox:first'),
      $select = $combobox.find('select:first'),
      $selectedcheckboxes = $combobox.find('input:checked'),
      value = null,
      values = [];

    var selectedcheckboxesInd = $selectedcheckboxes.length;
    while (selectedcheckboxesInd--) {
      value = $selectedcheckboxes[selectedcheckboxesInd].parentNode.getAttribute('data-value');
      if (value && value != -1) {
        values.unshift(value);
      }
    }

    if (values) {
      $select.val(values).change();
    }
  }

  function renderCombobox (select, options) {
    var
      html = [],
      option = null,
      defaultoption = null,
      optionsvalue = [],
      selectclassname = select.className,
      selectvalues = jQuery(select).val(),
      selectvalue = '',
      optionid = "",
      optionstitles = [],
      optionclassnames = [],
      selectoptionInd = -1,
      isselected = false,
      ismultiple = select.multiple === true,
      selectoption = null;

    selectvalues = typeof selectvalues === 'string' ? [selectvalues] : selectvalues || [];
    selectvalue = [].concat([''], selectvalues, ['']).join('xyu');

    for (var i = 0, n = options.length; i < n; i++) {
      option = options[i];
      if (option.value == -1) {
        defaultoption = option;
      }
      isselected = false;
      if (option.value != -1 && selectvalue.indexOf('xyu' + option.value + 'xyu') !== -1) {
        isselected = true;
        selectoption = option;
        optionstitles.unshift(selectoption.title);
        optionclassnames.unshift(selectoption.classname);
      }
      optionid = Math.floor(Math.random() * 1000000);
      selectoptionInd = i;
      optionsvalue.push({value : option.value, title : converText(option.title)});
      html = html.concat([
        '<li',
          ' class="option-item',
            option.classname ? ' ' + option.classname : '',
            option.selected === true ? ' selected-item' : '',
          '"',
          ' data-value="' + (optionsvalue.length - 1) + '"',
          ' title="' + '' + '"',
        '>',
        ismultiple ? '<input id="option-' + optionid + '" type="checkbox"' + (isselected === true ? ' checked="checked"' : '') + ' /><label for="option-' + optionid + '">' + option.title + '</label>' : option.title,
        '</li>'
      ]);
    }

    if (defaultoption && optionstitles.length === 0) {
      optionstitles.unshift(defaultoption.title);
    }

    html = [
      '<span class="combobox-title">',
        '<span class="inner-text combobox-title-inner-text">',
          optionstitles.length > 0 ? optionstitles.length === 1 ? optionstitles[0] : optionstitles.join(', ') || '' : '',
        '</span>',
      '</span>',
      '<span class="combobox-wrapper">',
        '<div class="combobox-container">',
          '<div class="container-top"></div>',
          '<ul class="combobox-options' + (ismultiple ? ' is-multiple' : '') + '">',
            html.join(''),
          '</ul>',
        '</div>',
      '</span>'
    ];

    var o = doc.createElement('span');
    o.className = 'custom-combobox' + (selectoptionInd !== -1 ? ' select-value-' + selectoptionInd : '') + (selectclassname ? ' ' + selectclassname : '') + (optionclassnames.length > 0 ? ' ' + optionclassnames.join(' ') : '');
    if (optionclassnames) {
      o.setAttribute('data-optclass', optionclassnames.join(' '));
    }
    o.innerHTML = html.join('');

    var
      optionsvalueLen = optionsvalue.length,
      value = null,
      node = null,
      nodes = o.getElementsByTagName('li'),
      nodesInd = 0;
    nodesInd = nodes ? nodes.length : 0;
    while (nodesInd--) {
      node = nodes[nodesInd];
      value = node.getAttribute('data-value');
      value = isFinite(+value) ? +value : -1;
      if (value > -1 && value < optionsvalueLen) {
        node.setAttribute('title', optionsvalue[value].title);
        node.setAttribute('data-value', optionsvalue[value].value);
      }
    }

    return o;
  }

  function updateSelect (select) {
    var
      selectvalue = select.value,
      opts = [],
      optionvalue = null,
      options = null,
      optionsInd = 0,
      option = null;

    options = select.getElementsByTagName('option');
    optionsInd = options ? options.length : 0;
    while (optionsInd--) {
      option = options[optionsInd];
      optionvalue = option.getAttribute('value');
      opts.unshift({classname : option.className, value : optionvalue, title : option.innerHTML, selected : optionvalue == selectvalue});
    }

    var o = renderCombobox(select, opts);
    if (o) {
      select.parentNode.insertBefore(o, select);
      o.appendChild(select);
      o.setAttribute('data-value', selectvalue);
      return o;
    }
    return null;
  }

  function setBodyEvents () {
    jQuery(document.body).one('click', onBodyClick);
  }

  function setEvents ($select, $combobox) {
    $select.blur(onSelectBlur).focus(onSelectFocus).change(onSelectChange);
    $combobox.find('span.combobox-title:first').click(onComboboxTitleClick);
    $combobox.find('ul.combobox-options:first').click(onComboboxOptionClick);
    $combobox.find('input').click(onComboboxCheckboxClick);
  }

  $.fn.advansedFilterCustomCombobox = function () {
    var
      select = null,
      combobox = null,
      $selects = $(this),
      selectsInd = 0,
      $select = null;

    selectsInd = $selects.length;
    while (selectsInd--) {
      select = $selects[selectsInd];
      $select = $(select);
      if ($select.hasClass('custom-combobox')) {
        continue;
      }

      combobox = updateSelect(select);
      setEvents($(select), $(combobox));
      $select.addClass('custom-combobox');

      $(combobox).find('div.combobox-container:first').append($select);

      if ($.browser.mobile) {
        $select.addClass('mobile-mode');
        $select.parents('.custom-combobox:first').addClass('mobile-mode');
      }
    }

    return $selects;
  };
})(jQuery, window, document, document.body);
