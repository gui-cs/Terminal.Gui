// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
exports.transform = function (model) {
    var groupNames = {
        "constructor": { key: "constructorsInSubtitle" },
        "field":       { key: "fieldsInSubtitle" },
        "property":    { key: "propertiesInSubtitle" },
        "method":      { key: "methodsInSubtitle" },
        "event":       { key: "eventsInSubtitle" },
        "operator":    { key: "operatorsInSubtitle" },
        "eii":         { key: "eiisInSubtitle" },
    };

    groupChildren(model);
    transformItem(model, 1);
    return model;

    function groupChildren(item) {
        if (!item || !item.items || item.items.length == 0) {
            return;
        }
        var grouped = {};
        var items = [];
        item.items.forEach(function (element) {
            groupChildren(element);
            if (element.type) {
                var type = element.isEii ? "eii" : element.type.toLowerCase();
                if (!grouped.hasOwnProperty(type)) {
                    if (!groupNames.hasOwnProperty(type)) {
                        groupNames[type] = {
                            name: element.type
                        };
                        console.log(type + " is not predefined type, use its type name as display name.")
                    }
                    grouped[type] = [];
                }
                grouped[type].push(element);
            } else {
                items.push(element);
            }
        }, this);
        
        // With order defined in groupNames
        for (var key in groupNames) {
            if (groupNames.hasOwnProperty(key) && grouped.hasOwnProperty(key)) {
                items.push({
                    name: model.__global[groupNames[key].key] || groupNames[key].name,
                    items: grouped[key]
                })
            }
        }

        item.items = items;
    }

    function transformItem(item, level) {
        // set to null in case mustache looks up
        item.topicHref = item.topicHref || null;
        item.tocHref = item.tocHref || null;
        item.name = item.name || null;

        item.level = level;

        // Add word break opportunities before dots

        if (item.name)
            item.name = item.name.replace(/\./g, "\u200B.");

        if (item.items && item.items.length > 0) {
            item.leaf = false;
            var length = item.items.length;
            for (var i = 0; i < length; i++) {
                transformItem(item.items[i], level + 1);
            };
        } else {
            item.items = [];
            item.leaf = true;
        }
    }
}