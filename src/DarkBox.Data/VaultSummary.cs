using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DarkBox.Data;

public sealed class VaultSummary : INotifyPropertyChanged
{
    private string _username;
    private string _editUsername;
    private bool _isEditing;

    public VaultSummary(long id, string username, int itemCount, bool isAdmin)
    {
        Id = id;
        _username = username;
        _editUsername = username;
        ItemCount = itemCount;
        IsAdmin = isAdmin;
    }

    public long Id { get; }

    public string Username
    {
        get => _username;
        set
        {
            if (_username == value)
                return;

            _username = value;
            OnPropertyChanged();
        }
    }

    public int ItemCount { get; }

    public bool IsAdmin { get; }

    public bool CanRename { get; set; }

    public bool CanDelete { get; set; }

    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (_isEditing == value)
                return;

            _isEditing = value;
            OnPropertyChanged();
        }
    }

    public string EditUsername
    {
        get => _editUsername;
        set
        {
            if (_editUsername == value)
                return;

            _editUsername = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
