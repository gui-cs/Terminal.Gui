// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
var common = require('./ManagedReference.common.js');

exports.postTransform = function (model) {
    var type = model.type.toLowerCase();
    var category = common.getCategory(type);
    if (category == 'class') {
        var typePropertyName = common.getTypePropertyName(type);
        if (typePropertyName) {
            model[typePropertyName] = true;
        }
        if (model.children && model.children.length > 0) {
            model.isCollection = true;
            common.groupChildren(model, 'class');
        } else {
            model.isItem = true;
        }
    }
    return model;
}