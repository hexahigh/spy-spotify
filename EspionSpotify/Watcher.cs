﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NAudio.Lame;

namespace EspionSpotify
{
    internal class Watcher
    {
        public static bool Running;
        public static bool Ready = true;
        public int CountSecs;

        private Recorder _recorder;
        private Song _lastSong;
        private Song _currentSong;

        private readonly FrmEspionSpotify _espionSpotifyForm;
        private readonly LAMEPreset _bitrate;
        private readonly Recorder.Format _format;
        private readonly Process _process2Spy;
        private readonly VolumeWin _sound;

        private readonly bool _bCdTrack;
        private readonly bool _strucDossiers;
        private readonly int _minTime;
        private readonly string _path;
        private readonly string _charSeparator;
        private readonly string[] _titleSeperators;

        private bool _bWait;
        private bool _bOnCommercialBreak;
        private string _lastTitle;
        private string _title;

        private const bool Unmute = false;

        public int NumTrack { get; private set; }

        private bool NumTrackActivated => NumTrack != -1;
        private bool CommercialOrNothingPlays => _lastSong != null && _currentSong == null && _title != null;
        private bool NewSongIsPlaying => _currentSong != null && !_currentSong.Equals(_lastSong);
        private bool SpotifyClosedOrCrashed => _currentSong == null && _title == null;

        private static bool SoundDetected
        {
            get
            {
                Thread.Sleep(500);
                return (int)(new VolumeWin().DefaultAudioEndPointDevice.AudioMeterInformation.MasterPeakValue * 100) > 0;
            }
        }

        public Watcher(FrmEspionSpotify espionSpotifyForm, string path, LAMEPreset bitrate,
            Recorder.Format format, VolumeWin sound, int minTime, bool strucDossiers, 
            string charSeparator, bool bCdTrack, bool bNumFile, int cdNumTrack)
        {
            if (path == null) path = "";

            _titleSeperators = new [] {" - "};
            _espionSpotifyForm = espionSpotifyForm;
            _path = path;
            _bitrate = bitrate;
            _format = format;
            _sound = sound;
            NumTrack = bCdTrack || bNumFile ? cdNumTrack : -1;
            _minTime = minTime;
            _strucDossiers = strucDossiers;
            _charSeparator = charSeparator;
            _bCdTrack = bCdTrack;
            _process2Spy = FindProcess();
            _title = GetTitle(_process2Spy);
        }

        public void Run()
        {
            if (Running) return;

            Ready = false;
            Running = true;

            _espionSpotifyForm.PrintStatusLine("//Début de l\'espionnage.");

            SpotifyStatusBeforeSpying();

            while (Running)
            {
                if (_bWait)
                {
                    WaitUntilSpotifyStartPlaying();
                    _sound.SetToHigh(Unmute, _title);
                }
                else
                {
                    StartRecordingSpotify();
                }
            }

            if (_recorder != null) DoIKeepLastSong(true);

            _espionSpotifyForm.PrintStatusLine("//Fin de l\'espionnage.");
            _espionSpotifyForm.PrintCurrentlyPlaying("");
            Ready = true;

            _sound.SetToHigh(!Unmute, _title);
        }

        private void SpotifyStatusBeforeSpying()
        {
            if (_title == null)
            {
                _espionSpotifyForm.PrintStatusLine("//Veuillez démarrer l\'application Spotify.");
                Running = false;
            }
            else
            {
                if (_title != "Spotify")
                {
                    _espionSpotifyForm.PrintStatusLine("//En attente du prochain titre...");
                    _bWait = true;
                }
                else
                {
                    _sound.SetToHigh(Unmute, _title);
                }
            }
        }

        private void WaitUntilSpotifyStartPlaying()
        {
            while (_bWait && Running)
            {
                _lastTitle = _title;
                _title = GetTitle(_process2Spy);

                if (_title != _lastTitle) _bWait = false;

                Thread.Sleep(20);
            }
        }

        private void StartRecordingSpotify()
        {
            _title = GetTitle(_process2Spy);
            _currentSong = GetSong(_title);

            if (CommercialOrNothingPlays)
            {
                _lastSong = null;
                DoIKeepLastSong(true, true);

                if (SoundDetected)
                {
                    SpotifyOnCommercialBreak();
                }
            }

            if (NewSongIsPlaying)
            {
                _lastSong = _currentSong;

                if (_bOnCommercialBreak) SpotifyLeftCommercialBreak();
                if (_recorder != null) DoIKeepLastSong();

                UpdateNum();

                _recorder = new Recorder(_espionSpotifyForm, _path, _bitrate, _format, _currentSong, _minTime,
                    _strucDossiers, _charSeparator, _bCdTrack, NumTrack);

                var recorderThread = new Thread(_recorder.Run);
                recorderThread.Start();

                CountSecs = 0;
            }

            if (SpotifyClosedOrCrashed)
            {
                _espionSpotifyForm.PrintStatusLine("//Spotify est fermé.");

                if (_recorder != null)
                {
                    DoIKeepLastSong(true, false, true);
                    _process2Spy.Dispose();
                }

                Running = false;
            }

            Thread.Sleep(100);
        }

        private void DoIKeepLastSong(bool updateUi = false, bool thenReset = false, bool deleteItAnyway = false)
        {
            _recorder.Count = deleteItAnyway ? -1 : CountSecs;
            _recorder.Running = false;

            if (updateUi) UpdateNum();
            if (thenReset) CountSecs = 0;
        }

        private void UpdateNum()
        {
            if (!NumTrackActivated) return;

            if (CountSecs < _minTime) NumTrack--;

            NumTrack++;
            _espionSpotifyForm.UpdateCdTrackNum(NumTrack);
        }

        private void SpotifyOnCommercialBreak()
        {
            _espionSpotifyForm.PrintStatusLine($"Publicité: {_title}");
            _bOnCommercialBreak = true;
            //_sound.DefaultAudioDeviceVolume = (int)(_sound.DefaultAudioDeviceVolume * 0.5);
        }

        private void SpotifyLeftCommercialBreak()
        {
            _bOnCommercialBreak = false;
            //_sound.DefaultAudioDeviceVolume = (int)(_sound.DefaultAudioDeviceVolume * 2);
        }

        private string GetTitle(Process process2Spy)
        {
            var process = GetProcess(process2Spy);

            if (process == null) return null;
            
            var title = process.MainWindowTitle;
            _espionSpotifyForm.PrintCurrentlyPlaying(title != "Spotify" ? title : "");

            return title;
        }

        private Song GetSong(string title)
        {
            var tags = title?.Split(_titleSeperators, 2, StringSplitOptions.None);
            return tags?.Length != 2 ? null : new Song(tags[0], tags[1]);
        }

        private static Process GetProcess(Process process2Spy)
        {
            var processlist = Process.GetProcesses();
            return process2Spy == null ? null : processlist.FirstOrDefault(process => process.Id == process2Spy.Id);
        }

        private static Process FindProcess()
        {
            var processlist = Process.GetProcesses();
            return processlist.FirstOrDefault(process => process.ProcessName.Equals("Spotify") && !string.IsNullOrEmpty(process.MainWindowTitle));
        }
    }
}
