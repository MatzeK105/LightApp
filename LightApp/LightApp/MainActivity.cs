using Android.App;
using Android.Widget;
using Android.OS;
using Android.Bluetooth;
using Android.Runtime;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Android.Content;
using System.Diagnostics;

namespace LightApp
{
    [Activity(Label = "LightApp", MainLauncher = false, Theme = "@style/MyTheme")]
    public class MainActivity : Activity
    {
        private bool connectedToArduino;
        private ImageView lightSwitch;
        private TextView nameTV;
        private TextView textView2;
        private Toolbar toolbar;
        private ImageView reloadBtn;
        private BluetoothDevice bleDevice;
        private BluetoothGatt bleGatt;
        private GattClientObserver observer;
        private BluetoothAdapter adapter;
        private BluetoothGattService lightService = null;
        private BluetoothGattCharacteristic lightCharacteristic = null;
        private readonly byte[] ARDUINO_MAC_ADDRESS = { 0x50, 0xF1, 0x4A, 0x50, 0x9A, 0x7E };
        //private readonly byte[] ARDUINO_MAC_ADDRESS = { 0xC8, 0x0F, 0x10, 0x69, 0xD5, 0xB6 }; // actually of Mi Band 1S, just for test purposes
        private const string LIGHT_SERVICE_UUID = "0000FFE0-0000-1000-8000-00805F9B34FB";
        private const string LIGHT_CHARACTERISTIC_UUID = "0000FFE1-0000-1000-8000-00805F9B34FB";

        public BluetoothGattCallback GattCallback { get; private set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            connectedToArduino = false;
            lightSwitch = FindViewById<ImageView>(Resource.Id.lightSwitchBtn);
            nameTV = FindViewById<TextView>(Resource.Id.textView1);
            textView2 = FindViewById<TextView>(Resource.Id.textView2);
            toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            reloadBtn = FindViewById<ImageView>(Resource.Id.reloadBtn);

            SetActionBar(toolbar);
            ActionBar.Title = "LightApp";
            
            // to run the code only once (at startup) or only when there is no connection to the arduino
            if (!connectedToArduino)
            {
                Main();
            }

            reloadBtn.Click += delegate
            {
                Main();
            };
        }

        private void Main()
        {
            if (Init())
            {
                connectedToArduino = ConnectToArduino();
            }
            else
            {
                textView2.Text = "Init failed";
            }

            if (connectedToArduino)
            {
                lightSwitch.Click += delegate
                {
                    lightCharacteristic.SetValue("1");
                    bleGatt.WriteCharacteristic(lightCharacteristic);
                };
            }
        }

        private bool Init()
        {
            adapter = BluetoothAdapter.DefaultAdapter;
            observer = new GattClientObserver();

            if (adapter != null)
            {
                if (observer != null)
                {
                    if (!adapter.IsEnabled)
                    {
                        adapter.Enable();

                        while (adapter.State != State.On)
                        {
                            //wait
                            WaitMs(10);
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        private bool ConnectToArduino()
        {
            if (ConnectToAddress(ARDUINO_MAC_ADDRESS))
            {
                if (GetService(LIGHT_SERVICE_UUID, ref lightService))
                {
                    if (GetCharacteristic(LIGHT_CHARACTERISTIC_UUID, lightService, ref lightCharacteristic))
                    {
                        // ready to work
                        nameTV.Text = bleDevice.Name;
                        textView2.Text = "Connected";
                        return true;
                    }
                    else
                    {
                        textView2.Text = "Get characteristic failed";
                    }
                }
                else
                {
                    textView2.Text = "Get service failed";
                }
            }
            else
            {
                textView2.Text = "Connecting to device failed";
            }

            return false;
        }

        private bool ConnectToAddress(byte[] address)
        {
            Stopwatch stopwatch = new Stopwatch();
            BluetoothManager manager;
        
            // find the device
            bleDevice = adapter.GetRemoteDevice(address);
            // connect to the device
            bleGatt = bleDevice.ConnectGatt(Application.Context, true, observer);

            manager = (BluetoothManager)GetSystemService(Context.BluetoothService);

            stopwatch.Start();

            // wait until the arduino is connected
            while (manager.GetConnectionState(bleDevice, ProfileType.Gatt) != ProfileState.Connected)
            {
                //after 5s return false
                if (stopwatch.ElapsedMilliseconds >= 5000)
                {
                    stopwatch.Stop();
                    return false;
                }

                WaitMs(100);
            }

            stopwatch.Stop();

            return true;
        }

        private bool GetService(string uuid, ref BluetoothGattService searchedService)
        {
            IList<BluetoothGattService> bleServicesList;
            Stopwatch stopwatch = new Stopwatch();

            // discover services of the device
            bleGatt.DiscoverServices();
            // get all services
            bleServicesList = bleGatt.Services;

            stopwatch.Start();
            
            while (bleServicesList.Count == 0)
            {
                if (stopwatch.ElapsedMilliseconds >= 3000)
                {
                    stopwatch.Stop();
                    return false;
                }

                WaitMs(100);
                bleServicesList = bleGatt.Services;
            }

            stopwatch.Stop();

            // if services were discovered, search for the service to switch the light and save the right characteristic
            if (bleServicesList.Count > 0)
            {
                foreach (BluetoothGattService service in bleServicesList)
                {
                    if (service.Uuid.ToString().ToUpper().Equals(uuid))
                    {
                        searchedService = service;
                        return true;
                    }
                }
            }

            return false;
        }

        private bool GetCharacteristic(string uuid, BluetoothGattService service, ref BluetoothGattCharacteristic searchedCharacteristic)
        {
            IList<BluetoothGattCharacteristic> bleCharacteristicsList;
            Stopwatch stopwatch = new Stopwatch();

            // get all characteristics of the service
            bleCharacteristicsList = service.Characteristics;

            stopwatch.Start();
            
            while (bleCharacteristicsList.Count == 0)
            {
                if (stopwatch.ElapsedMilliseconds >= 3000)
                {
                    stopwatch.Stop();
                    return false;
                }

                WaitMs(100);
                bleCharacteristicsList = service.Characteristics;
            }

            stopwatch.Stop();

            // search for the characteristic to switch the light
            if (bleCharacteristicsList.Count > 0)
            {
                foreach (BluetoothGattCharacteristic characteristic in bleCharacteristicsList)
                {
                    if (characteristic.Uuid.ToString().ToUpper().Equals(uuid))
                    {
                        searchedCharacteristic = characteristic;
                        return true;
                    }
                }
            }

            return false;
        }

        private void WaitMs(int ms)
        {
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            while (stopwatch.ElapsedMilliseconds < ms)
            {
                // wait
            }

            stopwatch.Stop();
        }
    }
}

