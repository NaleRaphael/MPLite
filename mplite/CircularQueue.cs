using System;
using System.Collections.Generic;
using System.Linq;

namespace MPLite
{
    using PlaybackMode = Core.PlaybackMode;
    using TrackInfo = Core.TrackInfo;
    using Playlist = Core.Playlist;

    public class CircularQueue<T> : Queue<T>
    {
        public int CurrentIndex { get; set; }

        public CircularQueue() : base()
        {
            CurrentIndex = -1;
        }

        public CircularQueue(int capacity) : base(capacity)
        {
            CurrentIndex = -1;
        }

        public T Next()
        {
            CurrentIndex = (CurrentIndex + 1 >= this.Count) ? 0 : CurrentIndex + 1;
            T result = this.ElementAt<T>(CurrentIndex);
            return result;
        }

        public T Previous()
        {
            CurrentIndex = (CurrentIndex - 1 < 0) ? this.Count - 1 : CurrentIndex - 1;
            T result = this.ElementAt<T>(CurrentIndex);
            return result;
        }

        public T Current()
        {
            return this.ElementAt<T>(CurrentIndex);
        }

        public new T Dequeue()
        {
            CurrentIndex = (CurrentIndex == 0) ? 0 : CurrentIndex - 1;
            return base.Dequeue();
        }

        public new void Clear()
        {
            CurrentIndex = -1;
            base.Clear();
        }
    }

    public class TrackQueue : CircularQueue<int>
    {
        public PlaybackMode Mode { get; private set; }
        public string ListName { get; private set; }
        public List<TrackInfo> Soundtracks { get; private set; }

        private int playedTrackAmount = 0;
        private int playedTrackAmountLimit = -1;

        public int TrackAmount
        {
            get { return this.Count; }
        }

        public TrackQueue(Playlist pl, int beginningIdx, PlaybackMode mode) : base(pl.TrackAmount)
        {
            Mode = mode;
            ListName = pl.ListName;
            Soundtracks = pl.Soundtracks;
            SetTrackQueue(pl.TrackAmount, beginningIdx, mode);
        }

        public int Next(out TrackInfo track)
        {
            int trackIdx = base.Next();
            track = null;

            switch (Mode)
            {
                case PlaybackMode.Default:
                case PlaybackMode.ShuffleOnce:
                case PlaybackMode.PlaySingle:
                    track = (playedTrackAmount++ >= playedTrackAmountLimit) ? null : this.Soundtracks[trackIdx];
                    break;
                case PlaybackMode.RepeatTrack:
                case PlaybackMode.RepeatList:
                case PlaybackMode.Shuffle:
                    track = this.Soundtracks[trackIdx];
                    playedTrackAmount++;
                    break;
                default:
                    // Throw exception?!
                    break;
            }
            Console.WriteLine("CurrentIndex: " + CurrentIndex);
            return trackIdx;
        }

        public TrackInfo GetNextTrack()
        {
            TrackInfo track;
            this.Next(out track);
            return track;
        }

        public int Current(out TrackInfo track)
        {
            int trackIdx = base.Current();
            track = this.Soundtracks[trackIdx];
            Console.WriteLine("CurrentIndex: " + CurrentIndex);
            return trackIdx;
        }

        public TrackInfo GetCurrentTrack()
        {
            int trackIdx = base.Current();
            return this.Soundtracks[trackIdx];
        }

        public int Previous(out TrackInfo track)
        {
            bool canPlayPreviousTrack = (playedTrackAmount - 1 <= 0);
            int trackIdx = canPlayPreviousTrack ? base.Current() : base.Previous();
            track = this.Soundtracks[trackIdx];
            playedTrackAmount = canPlayPreviousTrack ? playedTrackAmount : playedTrackAmount - 1;

            Console.WriteLine("CurrentIndex: " + CurrentIndex);
            return trackIdx;
        }

        public TrackInfo GetPreviousTrack()
        {
            TrackInfo track;
            this.Previous(out track);
            return track;
        }

        public new void Clear()
        {
            base.Clear();
            playedTrackAmount = 0;
            playedTrackAmountLimit = 0;
        }

        private void SetTrackQueue(int trackAmount, int beginningIdx, PlaybackMode mode)
        {
            this.Clear();
            playedTrackAmount = 0;

            // Reset beginningIdx if no track is selected
            beginningIdx = (beginningIdx == -1) ? 0 : beginningIdx;
            switch (mode)
            {
                case PlaybackMode.Default:
                    _SetTrackQueue_Default(trackAmount, beginningIdx);
                    playedTrackAmountLimit = trackAmount;
                    break;
                case PlaybackMode.RepeatList:
                    _SetTrackQueue_RepeatList(trackAmount, beginningIdx);
                    playedTrackAmountLimit = -1;
                    break;
                case PlaybackMode.Shuffle:
                    _SetTrackQueue_Shuffle(trackAmount, beginningIdx);
                    playedTrackAmountLimit = -1;
                    break;
                case PlaybackMode.ShuffleOnce:
                    _SetTrackQueue_Shuffle(trackAmount, beginningIdx);
                    playedTrackAmountLimit = trackAmount;
                    break;
                case PlaybackMode.PlaySingle:
                case PlaybackMode.RepeatTrack:
                    _SetTrackQueue_Single(trackAmount, beginningIdx);
                    playedTrackAmountLimit = 1;
                    break;
                default:
                    break;
            }
        }

        private void _SetTrackQueue_Default(int trackAmount, int beginningIdx)
        {
            for (int i = beginningIdx; i < trackAmount; i++)
                this.Enqueue(i);
        }

        private void _SetTrackQueue_RepeatList(int trackAmount, int beginningIdx)
        {
            for (int i = beginningIdx; i < trackAmount; i++)
                this.Enqueue(i);
            for (int i = 0; i < beginningIdx; i++)
                this.Enqueue(i);
        }

        private void _SetTrackQueue_Shuffle(int trackAmount, int beginningIdx)
        {
            // TODO: improve this
            Random rand = new Random();
            int[] ary = new int[trackAmount - 1];

            // Initialize array
            for (int i = 0; i < beginningIdx; i++)
                ary[i] = i;
            for (int i = beginningIdx + 1; i < trackAmount; i++)
                ary[i - 1] = i;

            // Swap randomly
            for (int i = 0; i < trackAmount - 1; i++)
            {
                int tempIdx = rand.Next(trackAmount - 1);
                int temp = ary[tempIdx];
                ary[tempIdx] = ary[i];
                ary[i] = temp;
            }

            this.Enqueue(beginningIdx);
            for (int i = 0; i < trackAmount - 1; i++)
                this.Enqueue(ary[i]);
        }

        private void _SetTrackQueue_Single(int trackAmount, int beginningIdx)
        {
            this.Enqueue(beginningIdx);
        }
    }
}
