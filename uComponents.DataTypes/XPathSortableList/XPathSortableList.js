﻿/*
    <div id="body_prop_xPathSortableListDocuments_ct100" class="xpath-sortable-list" 
         data-min-items="1"          
         data-max-items="3"
         data-allow-duplicates="false"
         data-type="">
 
        <ul class="source-list propertypane">            
            <li class="active" data-text="ABC" data-value="1">                
                <a class="add" title="add" href="javascript:void(0);" onclick="XPathSortableList.addItem(this);">
                    ABC
                </a>
            </li>

            <li data-text="XYZ" data-value="9">                
                <a class="add" title="add" href="javascript:void(0);" onclick="XPathSortableList.addItem(this);">
                    XYZ
                </a>)
            </li>
        </ul>

        <ul class="sortable-list propertypane">
            <li data-value="1">
                ABC
                <a class="delete" title="remove" href="javascript:void(0);" onclick="XPathSortableList.removeItem(this);"></a>
            </li>
            <li data-value="9">
                XYZ
                <a class="delete" title="remove" href="javascript:void(0);" onclick="XPathSortableList.removeItem(this);"></a>
            </li>
        </ul>
            
        <input type="hidden" name="ctl00$body$prop_sQLAutoComplete$ctl03">

    </div>

----------

    <XPathSortableList Type="c66ba18e-eaf3-4cff-8a22-41b16d66a972">
        <Item Value="1" />
        <Item Value="9" />
    </XPathSortableList>)

*/

var XPathSortableList = XPathSortableList || (function () {

    // public
    function init(div) {

        // dom
        var sourceUl = div.find('ul.source-list:first');
        var sortableUl = div.find('ul.sortable-list:first');
        var hidden = div.find('input:hidden:first');
       
        // data
        var type = div.data('type');
        var minItems = div.data('min-items');
        var maxItems = div.data('max-items');

        // no limit, so remove the border
        if (maxItems == 0) {
            sortableUl.css('border', '0');
        }

        // use the max value of either min or max and render that number of placeholder <li>s
        var liCount = Math.max(minItems, maxItems);
        for (var i = 1; i <= liCount; i++) {

            if (i <= minItems) {
                sortableUl.append('<li class="placeholder min">&nbsp;</li>');
            } else {
                sortableUl.append('<li class="placeholder max">&nbsp;</li>');
            }
        }

        // build selected items list from the hidden data
        if (hidden.val().length > 0) {
            var xml = jQuery.parseXML(hidden.val());

            jQuery(xml).find('Item').each(function (index, element) {

                var value = jQuery(element).attr('Value');
                var text = sourceUl.children('li[data-value=' + value + ']:first').data('text');

                addSortableListItem(sortableUl, text, value);
            });
        }

        // make list sortable
        sortableUl.sortable({
            items: 'li:not(.placeholder)',
            axis: 'y',
            update: function (event, ui) {
                updateHidden(sortableUl, hidden, type);
            }
        });

    }


    // public
    function addItem(a) {

        // dom
        var selectedLi = jQuery(a).parentsUntil('ul.source-list', 'li');
        var div = selectedLi.parents('div.xpath-sortable-list:first');

        var sortableUl = div.find('ul.sortable-list:first');
        var hidden = div.find('input:hidden:first');
       
        // data
        var type = div.data('type');
        var minItems = div.data('min-items');
        var maxItems = div.data('max-items');
        var allowDuplicates = (div.data('allow-duplicates') === 'True');
        
        // ensure won't exceed the max allowed
        if (maxItems == 0 || sortableUl.children('li:not(.placeholder)').length < maxItems) {

            if (allowDuplicates || sortableUl.children('li[data-value=' + selectedLi.data('value') + ']').length == 0) {

                if (!allowDuplicates) {
                    selectedLi.removeClass('active');
                }

                addSortableListItem(sortableUl, selectedLi.data('text'), selectedLi.data('value'));

                updateHidden(sortableUl, hidden, type);
            }
        }
    }
    
    // public
    function removeItem(a) {
       
        // dom        
        var li = jQuery(a).parentsUntil('ul.sortable-list', 'li');
        var sortableUl = li.parent();
        var hidden = sortableUl.siblings('input:hidden');
        var sourceUl = sortableUl.siblings('ul.source-list');

        // data
        var type = sortableUl.parent().data('type');
        var value = li.data('value');

        // re-activate the matching item by value in the source list
        sourceUl.children('li[data-value=' + value + ']').addClass('active');

        
        removeSortableListItem(li);
        

        // update the xml fragment
        updateHidden(sortableUl, hidden, type);
    }


    // adds an li to the sortable list
    function addSortableListItem(sortableUl, text, value) {

        //TODO: if there's an existing li of type placeholder? then remove it

        // handle placeholder <li>s
        var li = '<li data-value="' + value + '">' +
                            text + '<a class="delete" title="remove" href="javascript:void(0);" onclick="XPathSortableList.removeItem(this);"></a>' +
                 '</li>';


        // if there are any placeholders - replace the first one in the list
        var placeholder = sortableUl.children('li.placeholder')[0];
        if (placeholder) {

            jQuery(placeholder).replaceWith(li);

        } else {

            sortableUl.append(li);

        }
        
    }

    // ONLY ONE PLACE CALLS THIS
    function removeSortableListItem(li) {

        // dom
        var sortableUl = li.parent();
        var div = sortableUl.parent();
        
        
        // data
        var minItems = div.data('min-items');
        var maxItems = div.data('max-items');
        

        // handle placeholder <li>s

        li.remove();

        var count = sortableUl.children('li:not(.placeholder)').length;
        if (count < minItems) {
            sortableUl.append('<li class="placeholder min">&nbsp;</li>');
        } else if(count < maxItems) {
            sortableUl.append('<li class="placeholder max">&nbsp;</li>');
        }

    }


    //// private -- re-generates the xml fragment of selected items, and stores in the hidden field    
    function updateHidden(sortableUl, hidden, type) {

        var xml = '<XPathSortableList Type="' + type + '">';
        sortableUl.children('li:not(.placeholder)').each(function (index, element) {                
                xml += '<Item Value="' + jQuery(element).data('value') + '" />';
            });
            xml += '</XPathSortableList>';

        hidden.val(xml);
    }



    // public interface to the above methods
    return {

        init: init,
        addItem: addItem,
        removeItem: removeItem

    };

}());




