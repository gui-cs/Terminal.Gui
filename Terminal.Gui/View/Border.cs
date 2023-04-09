using NStack;
using System;
using System.Text.Json.Serialization;
using System.Data;
using System.Text;
using System.Collections.Generic;

namespace Terminal.Gui {


	/// <summary>
	/// Defines the visual border for a <see cref="Frame"/>. Also provides helper APIS for rendering the border.
	/// </summary>
	public class Border {

		/// <summary>
		/// Raised if any of the properties that define the border are changed.
		/// </summary>
		public event Action<Border> BorderChanged;

		private LineStyle _style;
		private Color _forgroundColor;
		private Color _backgroundColor;

		/// <summary>
		/// Specifies the <see cref="Gui.LineStyle"/> for a view.
		/// </summary>
		[JsonInclude, JsonConverter (typeof (JsonStringEnumConverter))]
		public LineStyle LineStyle {
			get => _style;
			set {
				_style = value;
				OnBorderChanged ();
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="Color"/> that draws the outer border color.
		/// </summary>
		[JsonInclude, JsonConverter (typeof (ColorJsonConverter))]
		public Color ForgroundColor {
			get => _forgroundColor;
			set {
				_forgroundColor = value;
				OnBorderChanged ();
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="Color"/> that fills the area between the bounds of a <see cref="Border"/>.
		/// </summary>
		[JsonInclude, JsonConverter (typeof (ColorJsonConverter))]
		public Color BackgroundColor {
			get => _backgroundColor;
			set {
				_backgroundColor = value;
				OnBorderChanged ();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public Thickness BorderThickness { get; set; } = new Thickness (0);
		/// <summary>
		/// 
		/// </summary>
		public object Effect3DBrush { get; set; }
		/// <summary>
		/// 
		/// </summary>
		public Thickness PaddingThickness { get; set; } = new Thickness (0);

		/// <summary>
		/// Invoke the <see cref="BorderChanged"/> event.
		/// </summary>
		public virtual void OnBorderChanged ()
		{
			BorderChanged?.Invoke (this);
		}
	}
}