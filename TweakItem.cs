using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ApexDebloater
{
    public class TweakItem : INotifyPropertyChanged
    {
        private bool _isApplied;
        private bool _isPending;

        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Category { get; set; }
        public required string Risk { get; set; } // "Safe", "Advanced"

        public bool IsApplied
        {
            get => _isApplied;
            set
            {
                if (_isApplied != value)
                {
                    _isApplied = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsPending
        {
            get => _isPending;
            set
            {
                if (_isPending != value)
                {
                    _isPending = value;
                    OnPropertyChanged();
                }
            }
        }

        public Action? ApplyAction { get; set; }
        public Action? UndoAction { get; set; }
        public Func<bool>? CheckAppliedAction { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
