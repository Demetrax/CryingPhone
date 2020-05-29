using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Stateless;
using CryingPhone.Models;
using System;
using System.Collections.Generic;
using System.Timers;
using Android.Media;
using Android.Content;
using System.Threading.Tasks;
using Xamarin.Essentials;


namespace CryingPhone
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        // UI Control
        TextView tvAppName;
        TextView tvInfo;

        Button btnVoice01;
        Button btnVoice02;

        // Media Player
        MediaPlayer mediaPlayer;
        AudioManager am = (AudioManager)Android.App.Application.Context.GetSystemService(Context.AudioService);

        // State machine
        public StateMachine<StateMach,Trigger> stateMachine;

        // Voice Array
        public List<int> voice1;
        public List<int> voice2;
        public List<int> voiceChoose;

        // Timer
        public Timer mainTimer;
        public Timer walkTimer;
        public Timer rollTimer;

        // counter
        public int shockCount;
        public int rollCount;

        // last Well
        public int lastWell;

        // Sensor
        private AccelerometerReader accelerometerReader = new AccelerometerReader();
        private OrientationReader orientationReader = new OrientationReader();
        private GyroscopeReader gyroscopeReader = new GyroscopeReader();

        // Const
        public const int timerSpeed = 500;
        public const int rollSensitivity = 45;

        // Start Datetime
        public DateTime startTimeRoll;
        public DateTime startTimeWalk;

        public bool gameAlreadyLaunch = false;
        public int timeForWell = 10;

        public int onCreateCount = 0;
        public int nbChangeState = 0;
        public int nbGame = 0;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            onCreateCount++;
            Console.WriteLine("---------------------------------------------------" + onCreateCount + "-----------------------------------------");
            tvAppName = FindViewById<TextView>(Resource.Id.tvAppName);
            tvAppName.Text = "Crying Phone `with state machine` ;) '";

            tvInfo = FindViewById<TextView>(Resource.Id.tvInfo);
            tvInfo.Text = "La voie 1 est sélectionné par défaut, Bouger le téléphone pour commencer";

            btnVoice01 = FindViewById<Button>(Resource.Id.btnVoice01);
            btnVoice01.Click += BtnVoice01_Click;

            btnVoice02 = FindViewById<Button>(Resource.Id.btnVoice02);
            btnVoice02.Click += BtnVoice02_Click;

            if (!gameAlreadyLaunch)
            {
                initVoice();

                accelerometerReader.ToggleAccelerometer();
                orientationReader.ToggleOrientationSensor();
                gyroscopeReader.ToggleGyroscope();

                //Delay3s();

                CreateStateMachine();

                MainLoop();
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        }

        private void initVoice()
        {
            voice1 = new List<int>();
            voice2 = new List<int>();
            voiceChoose = new List<int>();

            voice1.Add(Resource.Raw.Voice01_01);
            voice1.Add(Resource.Raw.Voice01_02);
            voice1.Add(Resource.Raw.Voice01_03);
            voice1.Add(Resource.Raw.Voice01_04);
            voice1.Add(Resource.Raw.Voice01_05);
            voice1.Add(Resource.Raw.Voice01_06);
            voice1.Add(Resource.Raw.Voice01_07);
            voice1.Add(Resource.Raw.Voice01_08);
            voice1.Add(Resource.Raw.Voice01_09);
            voice1.Add(Resource.Raw.Voice01_10);

            voice2.Add(Resource.Raw.Voice02_01);
            voice2.Add(Resource.Raw.Voice02_02);
            voice2.Add(Resource.Raw.Voice02_03);
            voice2.Add(Resource.Raw.Voice02_04);
            voice2.Add(Resource.Raw.Voice02_05);
            voice2.Add(Resource.Raw.Voice02_06);
            voice2.Add(Resource.Raw.Voice02_07);
            voice2.Add(Resource.Raw.Voice02_08);
            voice2.Add(Resource.Raw.Voice02_09);
            voice2.Add(Resource.Raw.Voice02_10);

            voiceChoose = voice1;
        }

        public async void Delay3s()
        {
            Console.WriteLine("Delay3s()");
            await Task.Delay(3000);

        }

        // Create and configure State Machine
        public void CreateStateMachine()
        {
            stateMachine = new StateMachine<StateMach, Trigger>(StateMach.Start);

            stateMachine.Configure(StateMach.Start)
                .Permit(Trigger.Go, StateMach.Walk);

            stateMachine.Configure(StateMach.Walk)
                .Permit(Trigger.Shocking, StateMach.Shock)
                .Permit(Trigger.Rolling, StateMach.Roll)
                .Permit(Trigger.Finish, StateMach.End)
                .OnEntry(OnWalking)
                .OnExit(OnWalkingOver);

            stateMachine.Configure(StateMach.Shock)
                .Permit(Trigger.ShockOver, StateMach.Walk)
                .OnEntry(OnShocked);

            stateMachine.Configure(StateMach.Roll)
                .Permit(Trigger.Shocking, StateMach.Shock)
                .Permit(Trigger.RollOver, StateMach.Walk)
                .OnEntry(OnRolling)
                .OnExit(OnRollingOver);

            stateMachine.Configure(StateMach.End)
                .Permit(Trigger.Go, StateMach.Start)
                .OnEntry(OnFinished);

            stateMachine.OnTransitioned(onTransitionAction);
        }

        private void onTransitionAction(StateMachine<StateMach, Trigger>.Transition obj)
        {
            Console.WriteLine("Changement Etat " + stateMachine.State);
            Console.WriteLine("nombre Changement Etat " + nbChangeState);
            nbChangeState++;
        }

        #region button event

        private void BtnVoice02_Click(object sender, EventArgs e)
        {
            voiceChoose = voice2;
        }

        private void BtnVoice01_Click(object sender, EventArgs e)
        {
            voiceChoose = voice1;
        }

        #endregion


        private void MainLoop()
        {
            if (!gameAlreadyLaunch)
            {

                gameAlreadyLaunch = true;
                Console.WriteLine("66666666666666666666666666666666666666666666666 Main Loop 66666666666666666666666666666666666666666666666666666666");
                mainTimer = new Timer();
                mainTimer.Start();
                mainTimer.Interval = timerSpeed;
                mainTimer.Enabled = true;

                mainTimer.Elapsed += MainTimerLoop;
            }
            
        }

        #region Timer Loop

        private void MainTimerLoop(object sender, ElapsedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!accelerometerReader.isStarted)
                {
                    accelerometerReader.ToggleAccelerometer();
                }

                if (!orientationReader.isStarted)
                {
                    orientationReader.ToggleOrientationSensor();
                }

                if (!gyroscopeReader.isStarted)
                {
                    gyroscopeReader.ToggleGyroscope();
                }

                if (((Math.Abs(gyroscopeReader.gyroX) + Math.Abs(gyroscopeReader.gyroY) + Math.Abs(gyroscopeReader.gyroZ)) > 3) && !am.IsMusicActive && nbGame < 1)
                {
                    //Game Start
                    Console.WriteLine("---------------------------The Game Begin-------------------------------------");
                    if (!am.IsMusicActive && nbGame<1)
                    {
                        nbGame++;
                        Console.WriteLine("---------------------------+++++++++++++++++++++++-------------------------------------" + nbGame);
                        mainTimer.Stop();
                        mainTimer.Enabled = false;
                        mediaPlayer = MediaPlayer.Create(this, voiceChoose[0]);
                        mediaPlayer.Start();
                        stateMachine.Fire(Trigger.Go);
                    }
                }
            });
        }

        private void WalkTimerLoop(object sender, ElapsedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() => {
                // Check if audio is not playing
                if (!am.IsMusicActive)
                {
                    // Detect Shock
                    if (accelerometerReader.shacked)
                    {
                        stateMachine.Fire(Trigger.Shocking);
                    }

                    // Detect roll or pitch
                    if (PitchOrRoll())
                    {
                        stateMachine.Fire(Trigger.Rolling);
                    }

                    // Check time for well
                    TimeSpan nbTime = e.SignalTime - startTimeRoll;
                    if (nbTime.Seconds > timeForWell && lastWell != 2)
                    {
                        mediaPlayer = MediaPlayer.Create(this, voiceChoose[8]);
                        mediaPlayer.Start();
                        lastWell = 2;
                    } 
                    else if (nbTime.Seconds > timeForWell/2 && lastWell != 1)
                    {
                        mediaPlayer = MediaPlayer.Create(this, voiceChoose[7]);
                        mediaPlayer.Start();
                        lastWell = 1;
                    }

                    // Detect End
                    if (OnTable())
                    {
                        stateMachine.Fire(Trigger.Finish);
                    }
                }

            });
        }


        private void RollTimerLoop(object sender, ElapsedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {

                if (accelerometerReader.shacked && !am.IsMusicActive)
                {
                    stateMachine.Fire(Trigger.Shocking);
                }

                double pitch = 180 * Math.Atan(accelerometerReader.accX / Math.Sqrt(accelerometerReader.accY * accelerometerReader.accY + accelerometerReader.accZ * accelerometerReader.accZ)) / Math.PI;
                double roll = 180 * Math.Atan(accelerometerReader.accY / Math.Sqrt(accelerometerReader.accX * accelerometerReader.accX + accelerometerReader.accZ * accelerometerReader.accZ)) / Math.PI;

                double newRoll = (Math.Abs(pitch) + Math.Abs(roll)) + 25;

                DateTime stopTime = e.SignalTime;
                TimeSpan nbTime = stopTime - startTimeRoll;

                if (nbTime.Seconds > 5)
                {
                    rollCount++;
                    switch (rollCount) 
                    {
                        case 1:
                            mediaPlayer = MediaPlayer.Create(this, voiceChoose[2]);
                            mediaPlayer.Start();
                            stateMachine.Fire(Trigger.RollOver);
                            break;
                        case 2:
                            mediaPlayer = MediaPlayer.Create(this, voiceChoose[3]);
                            mediaPlayer.Start();
                            stateMachine.Fire(Trigger.RollOver);
                            break;
                    }
                } 
                else if (newRoll < rollSensitivity)
                {
                    rollCount++;
                    switch (rollCount)
                    {
                        case 1:
                            mediaPlayer = MediaPlayer.Create(this, voiceChoose[1]);
                            mediaPlayer.Start();
                            stateMachine.Fire(Trigger.RollOver);
                            break;
                        case 2:
                            mediaPlayer = MediaPlayer.Create(this, voiceChoose[3]);
                            mediaPlayer.Start();
                            stateMachine.Fire(Trigger.RollOver);
                            break;
                    }
                }

            });
        }

        #endregion

        private void OnWalking()
        {
            Console.WriteLine("OnWalking");
            mainTimer.Dispose();

            startTimeWalk = DateTime.Now;
            timeForWell = 10;

            walkTimer = new Timer();
            walkTimer.Start();
            walkTimer.Interval = timerSpeed;
            walkTimer.Enabled = true;

            walkTimer.Elapsed += WalkTimerLoop;

        }


        private void OnWalkingOver()
        {
            Console.WriteLine("OnWalkingOver");
            walkTimer.Dispose();
        }

        private void OnShocked()
        {
            Console.WriteLine("OnShocked");
            accelerometerReader.shacked = false;
            shockCount++;

            switch (shockCount)
            {
                case 1:
                    mediaPlayer = MediaPlayer.Create(this, voiceChoose[4]);
                    mediaPlayer.Start();
                    break;
                case 2:
                    mediaPlayer = MediaPlayer.Create(this, voiceChoose[5]);
                    mediaPlayer.Start();
                    break;
                default:
                    mediaPlayer = MediaPlayer.Create(this, voiceChoose[6]);
                    mediaPlayer.Start();
                    break;
            }
            stateMachine.Fire(Trigger.ShockOver);
        }

        private void OnRolling()
        {
            Console.WriteLine("OnRolling");
            startTimeRoll = DateTime.Now;
            rollTimer = new Timer();
            rollTimer.Start();
            rollTimer.Enabled = true;

            rollTimer.Elapsed += RollTimerLoop;
        }



        private void OnRollingOver()
        {
            Console.WriteLine("OnRollingOver");
            rollTimer.Dispose();
            if (rollCount >= 2)
            {
                rollCount = 0;
            }
        }

        private void OnFinished()
        {
            Console.WriteLine("Well Done! Game Finish");
            mediaPlayer = MediaPlayer.Create(this, voiceChoose[9]);
            mediaPlayer.Start();
            stateMachine.Fire(Trigger.Go);
            Console.WriteLine("Game Restart");
            MainLoop();
        }

        #region UTILS

        private bool PitchOrRoll()
        {
            bool result = false;

            double pitch = 180 * Math.Atan(accelerometerReader.accX / Math.Sqrt(accelerometerReader.accY * accelerometerReader.accY + accelerometerReader.accZ * accelerometerReader.accZ)) / Math.PI;
            double roll = 180 * Math.Atan(accelerometerReader.accY / Math.Sqrt(accelerometerReader.accX * accelerometerReader.accX + accelerometerReader.accZ * accelerometerReader.accZ)) / Math.PI;

            if (Math.Abs(pitch) + Math.Abs(roll) > rollSensitivity)
            {
                result = true;
            }
            return result;
        }

        private bool OnTable()
        {
            bool result = false;

            double pitch = 180 * Math.Atan(accelerometerReader.accX / Math.Sqrt(accelerometerReader.accY * accelerometerReader.accY + accelerometerReader.accZ * accelerometerReader.accZ)) / Math.PI;
            double roll = 180 * Math.Atan(accelerometerReader.accY / Math.Sqrt(accelerometerReader.accX * accelerometerReader.accX + accelerometerReader.accZ * accelerometerReader.accZ)) / Math.PI;

            double gyro = (Math.Abs(gyroscopeReader.gyroX) + Math.Abs(gyroscopeReader.gyroY) + Math.Abs(gyroscopeReader.gyroZ)) * 100;

            if ((Math.Abs(pitch) + Math.Abs(roll) < 2) && (gyro < 1))
            {
                result = true;
            }
            return result;
        }

        #endregion
    }
}