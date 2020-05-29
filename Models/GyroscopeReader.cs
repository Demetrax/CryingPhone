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
    public class GyroscopeReader
    {
        // Set speed delay for monitoring changes.
        SensorSpeed speed = SensorSpeed.UI;

        public float gyroX, gyroY, gyroZ;

        public bool isStarted;

        public GyroscopeReader()
        {
            // Register for reading changes.
            Gyroscope.ReadingChanged += Gyroscope_ReadingChanged;
            gyroX = 0;
            gyroY = 0;
            gyroZ = 0;

            isStarted = false;
        }
        void Gyroscope_ReadingChanged(object sender, GyroscopeChangedEventArgs e)
        {
            var data = e.Reading;
            // Process Angular Velocity X, Y, and Z reported in rad/s
            //Console.WriteLine($"Reading: X: {data.AngularVelocity.X}, Y:{ data.AngularVelocity.Y}, Z: { data.AngularVelocity.Z}");
            gyroX = data.AngularVelocity.X;
            gyroY = data.AngularVelocity.Y;
            gyroZ = data.AngularVelocity.Z;

            if ((Math.Abs(data.AngularVelocity.X) + Math.Abs(data.AngularVelocity.Y) + Math.Abs(data.AngularVelocity.Z)) > 3)
            {
                //Console.WriteLine("------------ca tourne----------------- " + (Math.Abs(data.AngularVelocity.X) + Math.Abs(data.AngularVelocity.Y) + Math.Abs(data.AngularVelocity.Z)).ToString());
            }
        }

        public void ToggleGyroscope()
        {
            try
            {
                if (Gyroscope.IsMonitoring)
                {
                    Gyroscope.Stop();
                    isStarted = false;
                }
                else
                {
                    Gyroscope.Start(speed);
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