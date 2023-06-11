
using System;
using System.Collections.Generic;

namespace Terminal.Gui;

public class AccordionView : View
{
    private List<Section> _sections = new ();

    class Section
    {
        /// <summary>
        /// Typically 1 row in height, this is the view that can
        /// be clicked to expand.  It is a view not a string to allow
        /// cool stuff like left aligned text with right aligned buttons
        /// </summary>
        public View HeaderView { get; set; }


        public bool IsExpanded {get;set;}


        /// <summary>
        /// The view that will expand when the section is opened.
        /// </summary>
        public View BodyView {get;set;}
    }

    private bool _layoutInProgress;
    public override void LayoutSubviews ()
    {
        if(!_layoutInProgress)
        {
            try
            {
                _layoutInProgress = true;

                RemoveAll();
                View last = null;

                // TODO: Set body heights based on the count
                // of how many are expanded and/or dragged relative
                // positions (similar to TileView splitters)

                // TODO: Should we actually just use a vertical TileView for this maybe

                foreach(var s in _sections)
                {
                    var head = s.HeaderView;

                    head.Y = last == null ? 0 : Pos.Bottom(last);

                    last = head;

                    Add(head);

                    if(s.IsExpanded)
                    {
                        var body = s.BodyView;
                        body.Y = Pos.Bottom(last);
                        last = body;
                    }
                }
            }
            finally
            {
                _layoutInProgress = false;
            }
        }

        base.LayoutSubviews();        
    }
}