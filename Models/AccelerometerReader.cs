using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Essentials;

namespace CryingPhone.Models
{
    public class AccelerometerReader
    {
        // Set speed delay for monitoring changes.
        SensorSpeed speed = SensorSpeed.UI;

        public float accX, accY, accZ;

        public double mAccel, mAccelCurrent, mAccelLast;

        public bool isStarted;

        public bool shacked;

        public AccelerometerReader()
        {
            // Register for reading changes, be sure to unsubscribe when finished
            accX = 0;
            accY = 0;
            accZ = 0;
            isStarted = false;
            shacked = false;

            mAccel = 0.0f;
            mAccelCurrent = SensorManager.GravityEarth;
            mAccelLast = SensorManager.GravityEarth;


            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
            //Accelerometer.ShakeDetected += Accelerometer_ShakeDetected;
        }

        private void Accelerometer_ShakeDetected(object sender, EventArgs e)
        {
            //Console.WriteLine("----------------------Shake detected-----------------------");
            shacked = true;

        }

        void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
        {
            var data = e.Reading;
            //Console.WriteLine($"Reading: X: {data.Acceleration.X}, Y: {data.Acceleration.Y}, Z: {data.Acceleration.Z}");
            accX = data.Acceleration.X;
            accY = data.Acceleration.Y;
            accZ = data.Acceleration.Z;
            // Process Acceleration X, Y, and Z
            //calcule shack

            mAccelLast = mAccelCurrent;
            mAccelCurrent = Math.Sqrt(accX * accX + accY * accY + accZ * accZ);
            double delta = mAccelCurrent - mAccelLast;
            mAccel = mAccel * 0.9f + delta;
            if (mAccel > 0.75)
            {
                //Console.WriteLine("++++++++++++++++++++++++++++Shake detected++++++++++++++++++++++++++++++");
                shacked = true;
            }

        }

        public void ToggleAccelerometer()
        {
            try
            {
                if (Accelerometer.IsMonitoring)
                {
                    Accelerometer.Stop();
                    isStarted = false;
                }
                else
                {
                    Accelerometer.Start(speed);
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