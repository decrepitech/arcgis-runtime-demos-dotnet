﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ExternalNmeaGPS.Controls
{
	/// <summary>
	/// Interaction logic for SatelliteView.xaml
	/// </summary>
	public partial class SatelliteView : UserControl
	{
		public SatelliteView()
		{
			InitializeComponent();
		}

		public NmeaParser.Messages.Gsv GsvMessage
		{
			get { return (NmeaParser.Messages.Gsv)GetValue(GsvMessageProperty); }
			set { SetValue(GsvMessageProperty, value); }
		}

		public static readonly DependencyProperty GsvMessageProperty =
			DependencyProperty.Register(nameof(GsvMessage), typeof(NmeaParser.Messages.Gsv), typeof(SatelliteView), new PropertyMetadata(null, OnGsvMessagePropertyChanged));

		private static void OnGsvMessagePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var sats = e.NewValue as NmeaParser.Messages.Gsv;
			if (sats == null)
				((SatelliteView)d).satellites.ItemsSource = null;
			else
				((SatelliteView)d).satellites.ItemsSource = sats.SVs;
		}		
	}
	public class PolarPlacementItem : ContentControl
	{
		public PolarPlacementItem()
		{
			HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
			VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
		}
		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			double az = (Azimuth - 90) / 180 * Math.PI;
			double e = (90 - Elevation) / 90;
			double X = Math.Cos(az) * e;
			double Y = Math.Sin(az) * e;
			X = arrangeBounds.Width * .5 * X;
			Y = arrangeBounds.Height * .5 * Y;
			RenderTransform = new TranslateTransform(X, Y);
			return base.ArrangeOverride(arrangeBounds);
		}

		public double Azimuth
		{
			get { return (double)GetValue(AzimuthProperty); }
			set { SetValue(AzimuthProperty, value); }
		}

		public static readonly DependencyProperty AzimuthProperty =
			DependencyProperty.Register("Azimuth", typeof(double), typeof(PolarPlacementItem), new PropertyMetadata(0d, OnAzimuthPropertyChanged));

		private static void OnAzimuthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((UIElement)d).InvalidateArrange();
		}

		public double Elevation
		{
			get { return (double)GetValue(ElevationProperty); }
			set { SetValue(ElevationProperty, value); }
		}

		public static readonly DependencyProperty ElevationProperty =
			DependencyProperty.Register("Elevation", typeof(double), typeof(PolarPlacementItem), new PropertyMetadata(0d, OnElevationPropertyChanged));

		private static void OnElevationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((UIElement)d).InvalidateArrange();
		}

	}
}
