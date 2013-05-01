using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework.Audio;

namespace SpeakMemo
{
    public partial class MainPage : PhoneApplicationPage
    {
        // XNA objects for record and playback
        Microphone microphone;
        DynamicSoundEffectInstance playback;

        // Used for storing captured buffers
        List<byte[]> memoBufferCollection = new List<byte[]>();

        // Used for displaying stored memos
        ObservableCollection<MemoInfo> memoFiles = new ObservableCollection<MemoInfo>();

        // Data context of record button
        SpaceTime spaceTime = new SpaceTime();

        
        public MainPage()
        {
            InitializeComponent();
            // Create new Microphone and set event handler
            microphone = Microphone.Default;
            microphone.BufferReady += OnMicrophoneBufferReady;

            // Create new DynamicSoundEffectInstace for playback
            playback = new DynamicSoundEffectInstance(microphone.SampleRate, AudioChannels.Mono);
            playback.BufferNeeded += OnPlaybackBufferNeeded;

            // Enumerate existing memo waveform files in isolated storage
            using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                // Show filenames with most recent first
                string[] filenames = storage.GetFileNames();
                Array.Sort(filenames);
                Array.Reverse(filenames);

                foreach (string filename in filenames)
                {
                    using (IsolatedStorageFileStream stream = storage.OpenFile(filename, FileMode.Open, FileAccess.Read))
                    {
                        TimeSpan duration = microphone.GetSampleDuration((int)stream.Length);
                        MemoInfo memoInfo = new MemoInfo(filename, stream.Length, duration);
                        memoFiles.Add(memoInfo);
                    }
                }
            }

            // Set memo collection to ListBox
            memosListBox.ItemsSource = memoFiles;

            // Set-up record button
            recordButton.DataContext = spaceTime;
            UpdateRecordButton(false);
        }
        
        void UpdateRecordButton(bool isRecording)
        {
            if (!isRecording)
            {
                using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    spaceTime.Space = storage.AvailableFreeSpace;
                }
            }
            else
            {
                spaceTime.Space = memoBufferCollection.Count * microphone.GetSampleSizeInBytes(microphone.BufferDuration);
            }
            spaceTime.Time = microphone.GetSampleDuration((int)Math.Min(spaceTime.Space, Int32.MaxValue));

            recordButtonContent1.Visibility = isRecording ? Visibility.Collapsed : Visibility.Visible;
            recordButtonContent2.Visibility = isRecording ? Visibility.Visible : Visibility.Collapsed;
        }

        void OnRecordButtonClick(object sender, RoutedEventArgs args)
        {
            if (microphone.State == MicrophoneState.Stopped)
            {
                // Clear the collection for storing buffers
                memoBufferCollection.Clear();

                // Stop any playback in progress (not really necessary, but polite I guess)
                playback.Stop();

                // Start recording
                microphone.Start();
            }
            else
            {
                StopRecording();
            }

            // Update the record button
            bool isRecording = microphone.State == MicrophoneState.Started;
            UpdateRecordButton(isRecording);
        }

        void StopRecording()
        {
            // Get the last partial buffer
            int sampleSize = microphone.GetSampleSizeInBytes(microphone.BufferDuration);
            byte[] extraBuffer = new byte[sampleSize];
            int extraBytes = microphone.GetData(extraBuffer);

            // Stop recording
            microphone.Stop();

            // Create MemoInfo object and add at top of collection
            int totalSize = memoBufferCollection.Count * sampleSize + extraBytes;
            TimeSpan duration = microphone.GetSampleDuration(totalSize);
            MemoInfo memoInfo = new MemoInfo(DateTime.UtcNow, totalSize, duration);
            memoFiles.Insert(0, memoInfo);

            // Save data in isolated storage
            using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream stream = storage.CreateFile(memoInfo.FileName))
                {
                    // Write buffers from collection
                    foreach (byte[] buffer in memoBufferCollection)
                        stream.Write(buffer, 0, buffer.Length);

                    // Write partial buffer
                    stream.Write(extraBuffer, 0, extraBytes);
                }
            }

            // Scroll to show new MemoInfo item
            memosListBox.UpdateLayout();
            memosListBox.ScrollIntoView(memoInfo);
        }

        void OnMicrophoneBufferReady(object sender, EventArgs args)
        {
            // Get buffer from microphone and add to collection
            byte[] buffer = new byte[microphone.GetSampleSizeInBytes(microphone.BufferDuration)];
            int bytesReturned = microphone.GetData(buffer);
            memoBufferCollection.Add(buffer);

            UpdateRecordButton(true);

            // Check for 10-minute recording limit.
            // With the default sample rate of 16000, this is about 19M,
            //      which takes a few seconds to record to isolated storage.
            // Probably don't want to go much higher without saving the
            //      file incrementally, and providing more protection 
            //      against storage problems.
            if (spaceTime.Time > TimeSpan.FromMinutes(10))
            {
                StopRecording();
                UpdateRecordButton(false);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs args)
        {
            // If navigating from, stop recording
            if (microphone.State == MicrophoneState.Started)
            {
                StopRecording();
            }

            base.OnNavigatedFrom(args);
        }

        void OnPlayButtonClick(object sender, RoutedEventArgs args)
        {
            // Get clicked item
            Button btn = sender as Button;
            MemoInfo clickedMemoInfo = btn.Tag as MemoInfo;
            memosListBox.SelectedItem = clickedMemoInfo;

            // If playing, pause
            if (clickedMemoInfo.IsPlaying)
            {
                playback.Pause();
                clickedMemoInfo.IsPlaying = false;
                clickedMemoInfo.IsPaused = true;
            }
            // If paused, resume
            else if (clickedMemoInfo.IsPaused)
            {
                playback.Resume();
                clickedMemoInfo.IsPlaying = true;
                clickedMemoInfo.IsPaused = false;
            }
            // Otherwise, start playing
            else
            {
                // If another one is playing, stop it
                playback.Stop();

                // Fetch the waveform data from isolated storage and play it
                using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (IsolatedStorageFileStream stream = storage.OpenFile(clickedMemoInfo.FileName, FileMode.Open, FileAccess.Read))
                    {
                        byte[] buffer = new byte[stream.Length];
                        stream.Read(buffer, 0, buffer.Length);
                        playback.SubmitBuffer(buffer);
                    }
                }
                playback.Play();

                // Update the buttons in the ListBox
                foreach (MemoInfo memoInfo in memosListBox.Items)
                {
                    memoInfo.IsPlaying = memoInfo == clickedMemoInfo;
                    memoInfo.IsPaused = false;
                }
            }
        }

        void OnPlaybackBufferNeeded(object sender, EventArgs args)
        {
            // The whole buffer has been submitted for playing, 
            //  so this merely updates the play button if no buffers are pending
            if (playback.PendingBufferCount == 0)
                foreach (MemoInfo memoInfo in memosListBox.Items)
                {
                    memoInfo.IsPlaying = false;
                    memoInfo.IsPaused = false;
                }
        }

        void OnMemosListBoxSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            // Delete button is enabled only if item is selected
            deleteButton.IsEnabled = memosListBox.SelectedIndex != -1;
        }

        void OnDeleteButtonClick(object sender, RoutedEventArgs args)
        {
            // Show the confirm buttons
            deleteButtonContent1.Visibility = Visibility.Collapsed;
            deleteButtonContent2.Visibility = Visibility.Visible;
        }

        void OnDeleteButtonCancelClick(object sender, RoutedEventArgs args)
        {
            // Show the regular delete button
            deleteButtonContent1.Visibility = Visibility.Visible;
            deleteButtonContent2.Visibility = Visibility.Collapsed;
        }

        void OnDeleteButtonOkClick(object sender, RoutedEventArgs args)
        {
            // Get the selected item
            MemoInfo memoInfo = memosListBox.SelectedItem as MemoInfo;
            memosListBox.UpdateLayout();
            memosListBox.ScrollIntoView(memoInfo);

            // Stop it if it's playing
            if (memoInfo.IsPlaying || memoInfo.IsPaused)
                playback.Stop();

            // Delete it from isolated storage
            using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                storage.DeleteFile(memoInfo.FileName);
            }

            // Delete it from the local collection
            memoFiles.Remove(memoInfo);

            // Cleanup
            OnDeleteButtonCancelClick(sender, args);
        }
        
    }
}