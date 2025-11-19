# View Subclasses Still With `#nullable disable`

This document lists all View-related files in the `/Views` directory that still have `#nullable disable` set.

**Total**: 121 files

## Breakdown by Subdirectory

### Autocomplete (8 files)
- Autocomplete/AppendAutocomplete.cs
- Autocomplete/AutocompleteBase.cs
- Autocomplete/AutocompleteContext.cs
- Autocomplete/AutocompleteFilepathContext.cs
- Autocomplete/IAutocomplete.cs
- Autocomplete/ISuggestionGenerator.cs
- Autocomplete/SingleWordSuggestionGenerator.cs
- Autocomplete/Suggestion.cs

### CollectionNavigation (7 files)
- CollectionNavigation/CollectionNavigator.cs
- CollectionNavigation/CollectionNavigatorBase.cs
- CollectionNavigation/DefaultCollectionNavigatorMatcher.cs
- CollectionNavigation/ICollectionNavigator.cs
- CollectionNavigation/ICollectionNavigatorMatcher.cs
- CollectionNavigation/IListCollectionNavigator.cs
- CollectionNavigation/TableCollectionNavigator.cs

### Color/ColorPicker (13 files)
- Color/BBar.cs
- Color/ColorBar.cs
- Color/ColorModelStrategy.cs
- Color/ColorPicker.16.cs
- Color/ColorPicker.Prompt.cs
- Color/ColorPicker.Style.cs
- Color/ColorPicker.cs
- Color/GBar.cs
- Color/HueBar.cs
- Color/IColorBar.cs
- Color/LightnessBar.cs
- Color/RBar.cs
- Color/SaturationBar.cs
- Color/ValueBar.cs

### FileDialogs (10 files)
- FileDialogs/AllowedType.cs
- FileDialogs/DefaultFileOperations.cs
- FileDialogs/FileDialogCollectionNavigator.cs
- FileDialogs/FileDialogHistory.cs
- FileDialogs/FileDialogState.cs
- FileDialogs/FileDialogStyle.cs
- FileDialogs/FileDialogTableSource.cs
- FileDialogs/FilesSelectedEventArgs.cs
- FileDialogs/OpenDialog.cs
- FileDialogs/OpenMode.cs
- FileDialogs/SaveDialog.cs

### GraphView (9 files)
- GraphView/Axis.cs
- GraphView/BarSeriesBar.cs
- GraphView/GraphCellToRender.cs
- GraphView/GraphView.cs
- GraphView/IAnnotation.cs
- GraphView/LegendAnnotation.cs
- GraphView/LineF.cs
- GraphView/PathAnnotation.cs
- GraphView/TextAnnotation.cs

### Menu (3 files)
- Menu/MenuBarv2.cs
- Menu/Menuv2.cs
- Menu/PopoverMenu.cs

### Menuv1 (4 files)
- Menuv1/MenuClosingEventArgs.cs
- Menuv1/MenuItemCheckStyle.cs
- Menuv1/MenuOpenedEventArgs.cs
- Menuv1/MenuOpeningEventArgs.cs

### ScrollBar (2 files)
- ScrollBar/ScrollBar.cs
- ScrollBar/ScrollSlider.cs

### Selectors (2 files)
- Selectors/FlagSelector.cs
- Selectors/SelectorStyles.cs

### Slider (9 files)
- Slider/Slider.cs
- Slider/SliderAttributes.cs
- Slider/SliderConfiguration.cs
- Slider/SliderEventArgs.cs
- Slider/SliderOption.cs
- Slider/SliderOptionEventArgs.cs
- Slider/SliderStyle.cs
- Slider/SliderType.cs

### SpinnerView (2 files)
- SpinnerView/SpinnerStyle.cs
- SpinnerView/SpinnerView.cs

### TabView (4 files)
- TabView/Tab.cs
- TabView/TabChangedEventArgs.cs
- TabView/TabMouseEventArgs.cs
- TabView/TabStyle.cs

### TableView (18 files)
- TableView/CellActivatedEventArgs.cs
- TableView/CellColorGetterArgs.cs
- TableView/CellToggledEventArgs.cs
- TableView/CheckBoxTableSourceWrapper.cs
- TableView/CheckBoxTableSourceWrapperByIndex.cs
- TableView/CheckBoxTableSourceWrapperByObject.cs
- TableView/ColumnStyle.cs
- TableView/DataTableSource.cs
- TableView/EnumerableTableSource.cs
- TableView/IEnumerableTableSource.cs
- TableView/ITableSource.cs
- TableView/ListColumnStyle.cs
- TableView/ListTableSource.cs
- TableView/RowColorGetterArgs.cs
- TableView/SelectedCellChangedEventArgs.cs
- TableView/TableSelection.cs
- TableView/TableStyle.cs
- TableView/TableView.cs
- TableView/TreeTableSource.cs

### TextInput (11 files)
- TextInput/ContentsChangedEventArgs.cs
- TextInput/DateField.cs
- TextInput/HistoryTextItemEventArgs.cs
- TextInput/ITextValidateProvider.cs
- TextInput/NetMaskedTextProvider.cs
- TextInput/TextEditingLineStatus.cs
- TextInput/TextField.cs
- TextInput/TextRegexProvider.cs
- TextInput/TextValidateField.cs
- TextInput/TimeField.cs

### TreeView (14 files)
- TreeView/AspectGetterDelegate.cs
- TreeView/Branch.cs
- TreeView/DelegateTreeBuilder.cs
- TreeView/DrawTreeViewLineEventArgs.cs
- TreeView/ITreeBuilder.cs
- TreeView/ITreeViewFilter.cs
- TreeView/ObjectActivatedEventArgs.cs
- TreeView/SelectionChangedEventArgs.cs
- TreeView/TreeBuilder.cs
- TreeView/TreeNode.cs
- TreeView/TreeNodeBuilder.cs
- TreeView/TreeStyle.cs
- TreeView/TreeView.cs
- TreeView/TreeViewTextFilter.cs

### Wizard (3 files)
- Wizard/Wizard.cs
- Wizard/WizardEventArgs.cs
- Wizard/WizardStep.cs

## Summary

These 121 View-related files still have `#nullable disable` as they require additional work to be fully nullable-compliant. All other files in the Terminal.Gui library (outside of the Views directory) have been updated to support nullable reference types.
