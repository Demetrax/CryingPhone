using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Essentials;

namespace CryingPhone.Models
{
    public class OrientationReader
    {
        // Set speed delay for monitoring changes.
        SensorSpeed speed = SensorSpeed.UI;

        public float oriX, oriY, oriZ, oriW;

        public bool isStarted;

        public OrientationReader()
        {
            // Register for reading changes, be sure to unsubscribe when finished
            OrientationSensor.ReadingChanged += OrientationSensor_ReadingChanged;

            isStarted = false;

            oriX = 0;
            oriY = 0;
            oriZ = 0;
            oriW = 0;
        }
        void OrientationSensor_ReadingChanged(object sender, OrientationSensorChangedEventArgs e)
        {
            var data = e.Reading;
            //Console.WriteLine($"Reading: X: {data.Orientation.X}, Y: {data.Orientation.Y}, Z: { data.Orientation.Z}, W: { data.Orientation.W}");
            // Process Orientation quaternion (X, Y, Z, and W)
            oriX = data.Orientation.X;
            oriY = data.Orientation.Y;
            oriZ = data.Orientation.Z;
            oriW = data.Orientation.W;
        }
        public void ToggleOrientationSensor()
        {
            try
            {
                if (OrientationSensor.IsMonitoring)
                {
                    OrientationSensor.Stop();
                    isStarted = false;
                }
                else
                {
                    OrientationSensor.Start(speed);
                    isStarted = true;
                }

            }
            catch (FeatureNotSupportedException fnsEx)
            {
                // Feature not supported on device
                throw fnsEx.InnerException;
            }
            catch (Exception ex)
            {
                // Other error has occurred.
                throw ex.InnerException;
            }
        }
    }
}