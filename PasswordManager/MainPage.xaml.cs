using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace PasswordManager
{
    public partial class MainPage : ContentPage
    {
        private const string StorageKey = "password_records";
        private bool isLoaded;
        private bool isEntryPasswordVisible;

        public ObservableCollection<PasswordRecord> Entries { get; } = new();

        public List<string> Categories { get; } = new()
        {
            "Личное",
            "Работа",
            "Социальные сети",
            "Финансы",
            "Покупки",
            "Другое"
        };

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
            CategoryPicker.SelectedIndex = 0;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (isLoaded)
            {
                return;
            }

            try
            {
                string? json = await SecureStorage.Default.GetAsync(StorageKey);

                if (!string.IsNullOrWhiteSpace(json))
                {
                    List<PasswordRecord>? records =
                        JsonSerializer.Deserialize<List<PasswordRecord>>(json);

                    if (records is not null)
                    {
                        foreach (PasswordRecord record in records)
                        {
                            Entries.Add(record);
                        }
                    }
                }

                isLoaded = true;
            }
            catch (Exception)
            {
                await DisplayAlert("Ошибка", "Не удалось загрузить сохранённые записи.", "OK");
            }
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            string name = NameEntry.Text?.Trim() ?? string.Empty;
            string site = SiteEntry.Text?.Trim() ?? string.Empty;
            string password = PasswordEntry.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name)
                || string.IsNullOrWhiteSpace(site)
                || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Проверка данных", "Заполните название, сайт и пароль.", "OK");
                return;
            }

            PasswordRecord record = new()
            {
                Name = name,
                Site = site,
                Password = password,
                Category = CategoryPicker.SelectedItem as string ?? "Другое"
            };

            Entries.Add(record);

            try
            {
                string json = JsonSerializer.Serialize(Entries);
                await SecureStorage.Default.SetAsync(StorageKey, json);

                NameEntry.Text = string.Empty;
                SiteEntry.Text = string.Empty;
                PasswordEntry.Text = string.Empty;
                isEntryPasswordVisible = false;
                PasswordEntry.IsPassword = true;
                ToggleEntryPasswordButton.Text = "Показать";
            }
            catch (Exception)
            {
                Entries.Remove(record);
                await DisplayAlert("Ошибка", "Не удалось сохранить запись.", "OK");
            }
        }

        private async void OnOpenPasswordGeneratorClicked(object? sender, EventArgs e)
        {
            await Navigation.PushModalAsync(
                new NavigationPage(
                    new PasswordGeneratorPage(password =>
                    {
                        PasswordEntry.Text = password;
                        isEntryPasswordVisible = true;
                        PasswordEntry.IsPassword = false;
                        ToggleEntryPasswordButton.Text = "Скрыть";
                    })));
        }

        private void OnToggleEntryPasswordClicked(object? sender, EventArgs e)
        {
            isEntryPasswordVisible = !isEntryPasswordVisible;
            PasswordEntry.IsPassword = !isEntryPasswordVisible;
            ToggleEntryPasswordButton.Text =
                isEntryPasswordVisible ? "Скрыть" : "Показать";
        }

        private void OnTogglePasswordClicked(object? sender, EventArgs e)
        {
            if (sender is Button button
                && button.BindingContext is PasswordRecord record)
            {
                record.IsPasswordVisible = !record.IsPasswordVisible;
            }
        }

        private async void OnCopyPasswordClicked(object? sender, EventArgs e)
        {
            if (sender is not Button button
                || button.BindingContext is not PasswordRecord record)
            {
                return;
            }

            try
            {
                await Clipboard.Default.SetTextAsync(record.Password);
                await DisplayAlert("Скопировано", "Пароль скопирован в буфер обмена.", "OK");
            }
            catch (Exception)
            {
                await DisplayAlert("Ошибка", "Не удалось скопировать пароль.", "OK");
            }
        }
    }

    public class PasswordRecord : INotifyPropertyChanged
    {
        private bool isPasswordVisible;

        public string Name { get; set; } = string.Empty;

        public string Site { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Category { get; set; } = "Другое";

        [JsonIgnore]
        public string PasswordDisplay =>
            IsPasswordVisible ? Password : "••••••••";

        [JsonIgnore]
        public string ToggleText =>
            IsPasswordVisible ? "Скрыть" : "Показать";

        [JsonIgnore]
        public bool IsPasswordVisible
        {
            get => isPasswordVisible;
            set
            {
                if (isPasswordVisible == value)
                {
                    return;
                }

                isPasswordVisible = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PasswordDisplay));
                OnPropertyChanged(nameof(ToggleText));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
