// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
var common = require('./ManagedReference.common.js');

exports.getOptions = function (model) {
  var ignoreChildrenBookmarks = model._splitReference && model.type && common.getCategory(model.type) === 'ns';

  return {
    "bookmarks": common.getBookmarks(model, ignoreChildrenBookmarks)
  };
}