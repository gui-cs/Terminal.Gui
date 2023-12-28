// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

function toggleMenu() {
               
    var sidebar = document.getElementById("sidebar");
    var blackout = document.getElementById("blackout");

    if (sidebar.style.left === "0px") 
    {
        sidebar.style.left = "-" + sidebar.offsetWidth + "px";
        blackout.classList.remove("showThat");
        blackout.classList.add("hideThat");
    } 
    else 
    {
        sidebar.style.left = "0px";
        blackout.classList.remove("hideThat");
        blackout.classList.add("showThat");
    }
}

$(function() {
    $('table').each(function(a, tbl) {
        var currentTableRows = $(tbl).find('tbody tr').length;
        $(tbl).find('th').each(function(i) {
            var remove = 0;
            var currentTable = $(this).parents('table');

            var tds = currentTable.find('tr td:nth-child(' + (i + 1) + ')');
            tds.each(function(j) { if ($(this).text().trim() === '') remove++; });

            if (remove == currentTableRows) {
                $(this).hide();
                tds.hide();
            }
        });
    });

    function scrollToc() {
        var activeTocItem = $('.sidebar .sidebar-item.active:last')[0]
    
        if (activeTocItem) {
            activeTocItem.scrollIntoView({ block: "center" });
        }
        else{
            setTimeout(scrollToc, 500);
        }
    }

    setTimeout(scrollToc, 500);
});