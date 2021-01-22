# Tree View

TreeView is a control for navigating hierarchical objects.  It comes in two forms `TreeView` and `TreeView<T>`.  

## TreeView

The basic non generic TreeView class is populated by `ITreeNode` objects.  The simplest tree you can make would look something like:


```csharp
var tree = new TreeView()
{
    X = 0,
    Y = 0,
    Width = 40,
    Height = 20
};

Win.Add(tree);

var root1 = new TreeNode("Root1");
root1.Children.Add(new TreeNode("Child1.1"));
root1.Children.Add(new TreeNode("Child1.2"));

var root2 = new TreeNode("Root2");
root2.Children.Add(new TreeNode("Child2.1"));
root2.Children.Add(new TreeNode("Child2.2"));

tree.AddObject(root1);
tree.AddObject(root2);

```

Having to create a bunch of TreeNode objects can be a pain especially if you already have your own objects e.g. `House`, `Room` etc.  There are two ways to use your own classes without having to create nodes manually.  Firstly you can implement the `ITreeNode` interface:


```
// Your data class
private class House : ITreeNode {


    // Your properties
    public string Address {get;set;}
    public List<Room> Rooms {get;set;}

    // ITreeNode member:

    public IList<ITreeNode> Children => Rooms.Cast<ITreeNode>().ToList();
    
    public override string ToString ()
    {
        return Address;
    }
}

// Your other data class
private class Room : ITreeNode{
    
    public string Name {get;set;}


    // Rooms have no sub objects
    public IList<ITreeNode> Children => new List<ITreeNode>();

    public override string ToString ()
    {
        return Name;
    }
}


...

// After implementing the interface you can add your objects directly to the tree

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

Alternatively you can simply tell the tree how the objects relate to one another by implementing `ITreeBuilder`.  This is a good option if you don't have control of the data objects you are working with:

```
TODO
```

## TreeView<T>
