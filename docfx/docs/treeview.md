# Tree View

TreeView is a control for navigating hierarchical objects. It comes in two forms `TreeView` and `TreeView<T>`.

[TreeView API Reference](~/api/Terminal.Gui.Views.TreeView.yml)

## Using TreeView

The basic non generic TreeView class is populated by `ITreeNode` objects. The simplest tree you can make would look something like:

```csharp
var tree = new TreeView()
{
  X = 0,
  Y = 0,
  Width = 40,
  Height = 20
};

var root1 = new TreeNode("Root1");
root1.Children.Add(new TreeNode("Child1.1"));
root1.Children.Add(new TreeNode("Child1.2"));

var root2 = new TreeNode("Root2");
root2.Children.Add(new TreeNode("Child2.1"));
root2.Children.Add(new TreeNode("Child2.2"));

tree.AddObject(root1);
tree.AddObject(root2);

```

Having to create a bunch of TreeNode objects can be a pain especially if you already have your own objects e.g. `House`, `Room` etc. There are two ways to use your own classes without having to create nodes manually. Firstly you can implement the `ITreeNode` interface:


```csharp
// Your data class
private class House : TreeNode {

    // Your properties
    public string Address {get;set;}
    public List<Room> Rooms {get;set;}

    // ITreeNode member:
    public override IList<ITreeNode> Children => Rooms.Cast<ITreeNode>().ToList();

    public override string Text { get => Address; set => Address = value; }
}


// Your other data class
private class Room : TreeNode{

    public string Name {get;set;}

    public override string Text{get=>Name;set{Name=value;}}
}
```

After implementing the interface you can add your objects directly to the tree

```csharp

var myHouse = new House()
{
    Address = "23 Nowhere Street",
    Rooms = new List<Room>{
      new Room(){Name = "Ballroom"},
      new Room(){Name = "Bedroom 1"},
      new Room(){Name = "Bedroom 2"}
    }
};

var tree = new TreeView()
{
    X = 0,
    Y = 0,
    Width = 40,
    Height = 20
};

tree.AddObject(myHouse);

```

Alternatively you can simply tell the tree how the objects relate to one another by implementing `ITreeBuilder<T>`. This is a good option if you don't have control of the data objects you are working with.

## `TreeView<T>`

The generic `Treeview<T>` allows you to store any object hierarchy where nodes implement Type T. For example if you are working with `DirectoryInfo` and `FileInfo` objects then you could create a `TreeView<FileSystemInfo>`. If you don't have a shared interface/base class for all nodes you can still declare a `TreeView<object>`.

In order to use `TreeView<T>` you need to tell the tree how objects relate to one another (who are children of who). To do this you must provide an `ITreeBuilder<T>`.

### `Implementing ITreeBuilder<T>`

Consider a simple data model that already exists in your program:

```csharp
private abstract class GameObject
{

}

private class Army : GameObject
{
    public string Designation {get;set;}
    public List<Unit> Units {get;set;}


    public override string ToString ()
    {
        return Designation;
    }
}

private class Unit : GameObject
{
    public string Name {get;set;}
    public override string ToString ()
    {
        return Name;
    }
}

```

An `ITreeBuilder<T>` for these classes might look like:

```csharp

private class GameObjectTreeBuilder : ITreeBuilder<GameObject> {
    public bool SupportsCanExpand => true;

    public bool CanExpand (GameObject model)
    {
        return model is Army;
    }

    public IEnumerable<GameObject> GetChildren (GameObject model)
    {
        if(model is Army a)
            return a.Units;

        return Enumerable.Empty<GameObject>();
    }
}
```

To use the builder in a tree you would use:

```csharp
var army1 = new Army()
{
    Designation = "3rd Infantry",
    Units = new List<Unit>{
        new Unit(){Name = "Orc"},
        new Unit(){Name = "Troll"},
        new Unit(){Name = "Goblin"},
    }
};

var tree = new TreeView<GameObject>()
{
    X = 0,
    Y = 0,
    Width = 40,
    Height = 20,
    TreeBuilder = new GameObjectTreeBuilder()
};


tree.AddObject(army1);
```

Alternatively you can use `DelegateTreeBuilder<T>` instead of implementing your own `ITreeBuilder<T>`. For example:

```csharp
tree.TreeBuilder = new DelegateTreeBuilder<GameObject>(
    (o)=>o is Army a ? a.Units 
      : Enumerable.Empty<GameObject>());
```

## Node Text and ToString

The default behavior of TreeView is to use the `ToString` method on the objects for rendering. You can customise this by changing the `AspectGetter`. For example:

```csharp
treeViewFiles.AspectGetter = (f)=>f.FullName;
```
