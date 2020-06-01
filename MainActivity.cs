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

        #region VAR
        // UI Control
        TextView tvAppName;
        TextView tvInfo;

        Button btnVoice01;
        Button btnVoice02;
        Button btnVoiceOff;

        // Media Player
        MediaPlayer mediaPlayer;
        AudioManager am = (AudioManager)Android.App.Application.Context.GetSystemService(Context.AudioService);

        // State machine
        public StateMachine<StateMach,Trigger> stateMachine;

        // Voice Array
        public List<int> voice1;
        public List<int> voice2;
        public List<int> voiceChoose;

        // Text Array
        public List<string> alternativeText;

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
        public const int timerSpeed = 250;
        public const int rollSensitivity = 45;
        public const int timeForWell = 10;

        // Start Datetime
        public DateTime startTimeRoll;
        public DateTime startTimeWalk;
        public DateTime startTimeGame;

        // Status
        public bool gameAlreadyLaunch = false;
        public bool soundOff = false;
        

        // Debug
        public int nbChangeState = 0;
        public int nbGame = 0;

        #endregion

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            tvAppName = FindViewById<TextView>(Resource.Id.tvAppName);
            tvAppName.Text = "THE FABULOUS CRYING PHONE !";

            tvInfo = FindViewById<TextView>(Resource.Id.tvInfo);
            tvInfo.Text = "La voie 1 est sélectionnée par défaut, Bouger le téléphone pour commencer. Une partie dure minimum 20 sec, a la fin d'une partie, secoué le téléphone pour rejouer";

            btnVoice01 = FindViewById<Button>(Resource.Id.btnVoice01);
            btnVoice01.Click += BtnVoice01_Click;

            btnVoice02 = FindViewById<Button>(Resource.Id.btnVoice02);
            btnVoice02.Click += BtnVoice02_Click;

            btnVoiceOff = FindViewById<Button>(Resource.Id.btnVoiceOff);
            btnVoiceOff.Click += BtnVoiceOff_Click;

            if (!gameAlreadyLaunch)
            {
                InitVoice();

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

        private void InitVoice()
        {
            voice1 = new List<int>();
            voice2 = new List<int>();
            voiceChoose = new List<int>();
            alternativeText = new List<string>();

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

            alternativeText.Add("Peux tu me transporter sans me pencher, ni me faire tomber. Attention ca commence :=)");
            alternativeText.Add("Aaaah");
            alternativeText.Add("Aaaaaaaaaah, ca peeeeeeenche ! :/");
            alternativeText.Add("Oooooooooooh, Encore je vais vomir Oo");
            alternativeText.Add("Aieueu ! X_X");
            alternativeText.Add("Eh !! ca fait mal ! X_X");
            alternativeText.Add("Aieueuueueu !! snif snif tu ma fait mal ! X_X");
            alternativeText.Add("Humm Waouhh");
            alternativeText.Add("Waouhh tu es presque aussi doué que Fabio Lanzoni ;)");
            alternativeText.Add("Merci, tu es vraiment trop fort ! et trés beau ce qui ne gache rien ! <(^^,)>");

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


        #region BUTTON_EVENT

        private void BtnVoice02_Click(object sender, EventArgs e)
        {
            voiceChoose = voice2;
        }

        private void BtnVoice01_Click(object sender, EventArgs e)
        {
            voiceChoose = voice1;
        }

        private void BtnVoiceOff_Click(object sender, EventArgs e)
        {
            am.SetStreamMute(Stream.Music, !am.IsStreamMute(Stream.Music));
            soundOff = !soundOff;
            if (soundOff)
            {
                btnVoiceOff.Text = "VOICE ON";
            } else
            {
                btnVoiceOff.Text = "VOICE OFF";
            }
        }

        #endregion


        private void MainLoop()
        {
            if (!gameAlreadyLaunch)
            {

                gameAlreadyLaunch = true;
                Console.WriteLine("Main Loop");
                mainTimer = new Timer();
                mainTimer.Start();
                mainTimer.Interval = timerSpeed;
                mainTimer.Enabled = true;

                mainTimer.Elapsed += MainTimerLoop;
            }
        }

        #region TIMER_LOOP

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

                if (((Math.Abs(gyroscopeReader.gyroX) + Math.Abs(gyroscopeReader.gyroY) + Math.Abs(gyroscopeReader.gyroZ)) > 5) && !am.IsMusicActive)
                {
                    //Game Start
                    Console.WriteLine("---------------------------The Game Begin-------------------------------------");

                    nbGame++;
                    mainTimer.Stop();
                    mainTimer.Enabled = false;
                    mediaPlayer = MediaPlayer.Create(this, voiceChoose[0]);
                    tvInfo.Text = alternativeText[0];
                    mediaPlayer.Start();
                    startTimeGame = DateTime.Now;
                    stateMachine.Fire(Trigger.Go);
                }
            });
        }

        private void WalkTimerLoop(object sender, ElapsedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() => {
                // Check if audio is not playing
                if (!am.IsMusicActive)
                {
                    TimeSpan gameDuration = e.SignalTime - startTimeGame;
                    // Detect Shock
                    if (accelerometerReader.shacked)
                    {
                        stateMachine.Fire(Trigger.Shocking);
                    }
                    // Detect roll or pitch
                    else if (PitchOrRoll()) {
                        stateMachine.Fire(Trigger.Rolling);
                    } 
                    // Detect End
                    else if (OnTable() && (gameDuration.Seconds > 20))
                    {
                        stateMachine.Fire(Trigger.Finish);
                    }
                    // Check time for well
                    else
                    {
                        TimeSpan nbTime = e.SignalTime - startTimeWalk;
                        if (nbTime.Seconds > timeForWell && lastWell != 2)
                        {

                            mediaPlayer = MediaPlayer.Create(this, voiceChoose[8]);
                            tvInfo.Text = alternativeText[8];
                            mediaPlayer.Start();
                            lastWell = 2;
                            startTimeWalk = DateTime.Now;
                        }
                        else if (nbTime.Seconds > timeForWell / 2 && lastWell != 1)
                        {

                            mediaPlayer = MediaPlayer.Create(this, voiceChoose[7]);
                            tvInfo.Text = alternativeText[7];
                            mediaPlayer.Start();
                            lastWell = 1;
                            startTimeWalk = DateTime.Now;

                        }
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
                } else
                {
                    double pitch = 180 * Math.Atan(accelerometerReader.accX / Math.Sqrt(accelerometerReader.accY * accelerometerReader.accY + accelerometerReader.accZ * accelerometerReader.accZ)) / Math.PI;
                    double roll = 180 * Math.Atan(accelerometerReader.accY / Math.Sqrt(accelerometerReader.accX * accelerometerReader.accX + accelerometerReader.accZ * accelerometerReader.accZ)) / Math.PI;

                    double newRoll = (Math.Abs(pitch) + Math.Abs(roll)) + 25;

                    DateTime stopTime = e.SignalTime;
                    TimeSpan nbTime = stopTime - startTimeRoll;

                    if (nbTime.Seconds > 5 && !am.IsMusicActive)
                    {
                        rollCount++;
                        switch (rollCount)
                        {
                            case 1:
                                mediaPlayer = MediaPlayer.Create(this, voiceChoose[2]);
                                tvInfo.Text = alternativeText[2];
                                mediaPlayer.Start();
                                stateMachine.Fire(Trigger.RollOver);
                                break;
                            case 2:
                                mediaPlayer = MediaPlayer.Create(this, voiceChoose[3]);
                                tvInfo.Text = alternativeText[3];
                                mediaPlayer.Start();
                                stateMachine.Fire(Trigger.RollOver);
                                break;
                        }
                    }
                    else if (newRoll < rollSensitivity && !am.IsMusicActive)
                    {
                        rollCount++;
                        switch (rollCount)
                        {
                            case 1:
                                mediaPlayer = MediaPlayer.Create(this, voiceChoose[1]);
                                tvInfo.Text = alternativeText[1];
                                mediaPlayer.Start();
                                stateMachine.Fire(Trigger.RollOver);
                                break;
                            case 2:
                                mediaPlayer = MediaPlayer.Create(this, voiceChoose[3]);
                                tvInfo.Text = alternativeText[3];
                                mediaPlayer.Start();
                                stateMachine.Fire(Trigger.RollOver);
                                break;
                        }
                    }
                }

            });
        }

        #endregion

        #region STATE_MACHINE

        private void OnWalking()
        {
            Console.WriteLine("OnWalking");
            mainTimer.Dispose();

            startTimeWalk = DateTime.Now;

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
                    tvInfo.Text = alternativeText[4];
                    mediaPlayer.Start();
                    break;
                case 2:
                    mediaPlayer = MediaPlayer.Create(this, voiceChoose[5]);
                    tvInfo.Text = alternativeText[5];
                    mediaPlayer.Start();
                    break;
                default:
                    mediaPlayer = MediaPlayer.Create(this, voiceChoose[6]);
                    tvInfo.Text = alternativeText[6];
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
            tvInfo.Text = alternativeText[9];
            mediaPlayer.Start();
            stateMachine.Fire(Trigger.Go);
            Console.WriteLine("Game Restart");
            // reinitialize value
            shockCount = 0;
            rollCount = 0;
            gameAlreadyLaunch = false;
            MainLoop();
        }

        #endregion

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

            if (gyro < 2)
            {
                result = true;
            }
            return result;
        }

        public async void Delay3s()
        {
            Console.WriteLine("Delay3s()");
            await Task.Delay(3000);

        }

        private void onTransitionAction(StateMachine<StateMach, Trigger>.Transition obj)
        {
            Console.WriteLine("Changement Etat " + stateMachine.State);
            Console.WriteLine("nombre Changement Etat " + nbChangeState);
            nbChangeState++;
        }

        #endregion
    }
}