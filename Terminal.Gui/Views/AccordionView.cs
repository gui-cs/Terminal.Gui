
using System;
using System.Collections.Generic;

namespace Terminal.Gui;

public class AccordionView : View
{
    private List<Section> _sections = new ();

    class Section
    {
        public Button HeaderButton { get; set; }


        public bool IsExpanded {get;set;}


        /// <summary>
        /// The view that will expand when the section is opened.
        /// </summary>
        public View BodyView {get;set;}
    }

    public void AddSection(string header, View body)
    {
        Button btnExpand = new Button( "> " + header);
        btnExpand.NoDecorations = true;

        var section = new Section{
            HeaderButton = btnExpand,
            BodyView = body,
            IsExpanded = false
        };
        btnExpand.Clicked += (s,e)=> ExpandOrCollapseSection(section);

        _sections.Add(section);

        Setup();
    }

	private void ExpandOrCollapseSection (Section section)
	{
		section.IsExpanded = !section.IsExpanded;

        var symbol = section.IsExpanded ? "v ":"> ";
        section.HeaderButton.Text = symbol + section.HeaderButton.Text.Substring(2);
        
        Setup();
	}
    
	private void Setup ()
    {
        var focused = MostFocused;

        View last = null;
        RemoveAll();

        // TODO: Set body heights based on the count
        // of how many are expanded and/or dragged relative
        // positions (similar to TileView splitters)

        // TODO: Should we actually just use a vertical TileView for this maybe

        foreach(var s in _sections)
        {
            var head = s.HeaderButton;
            head.Y = last == null ? 0 : Pos.Bottom(last);
            Add(head);
            last = head;

            if(s.IsExpanded)
            {
                var body = s.BodyView;
                body.Y = Pos.Bottom(last);
                Add(body);
                last = body;
            }
        }
        this.LayoutSubviews();
        focused?.SetFocus();
        this.SetNeedsDisplay();
    }
}