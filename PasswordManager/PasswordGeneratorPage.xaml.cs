using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.Maui.Controls;

namespace PasswordManager
{
    public partial class PasswordGeneratorPage : ContentPage
    {
        private const int PasswordLength = 16;
        private readonly Action<string> passwordSelected;
        private bool isPasswordVisible;

        public List<string> ComplexityLevels { get; } = new()
        {
            "Простой",
            "Средний",
            "Сложный"
        };

        public PasswordGeneratorPage(Action<string> passwordSelected)
        {
            InitializeComponent();
            this.passwordSelected = passwordSelected;
            BindingContext = this;
            ComplexityPicker.SelectedIndex = 1;
        }

        private void OnGenerateClicked(object? sender, EventArgs e)
        {
            string complexity = ComplexityPicker.SelectedItem as string ?? "Средний";
            GeneratedPasswordEntry.Text = GeneratePassword(complexity);

            isPasswordVisible = true;
            GeneratedPasswordEntry.IsPassword = false;
            TogglePasswordButton.Text = "Скрыть";
        }

        private void OnTogglePasswordClicked(object? sender, EventArgs e)
        {
            isPasswordVisible = !isPasswordVisible;
            GeneratedPasswordEntry.IsPassword = !isPasswordVisible;
            TogglePasswordButton.Text = isPasswordVisible ? "Скрыть" : "Показать";
        }

        private async void OnUsePasswordClicked(object? sender, EventArgs e)
        {
            string password = GeneratedPasswordEntry.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Нет пароля", "Сначала сгенерируйте пароль.", "OK");
                return;
            }

            passwordSelected(password);
            await Navigation.PopModalAsync();
        }

        private async void OnCloseClicked(object? sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private static string GeneratePassword(string complexity)
        {
            const string lowerCase = "abcdefghijkmnopqrstuvwxyz";
            const string upperCase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string digits = "23456789";
            const string symbols = "!@#$%^&*()-_=+[]{}?";

            string pool;
            string[] requiredGroups;

            switch (complexity)
            {
                case "Простой":
                    pool = lowerCase + digits;
                    requiredGroups = new[] { lowerCase, digits };
                    break;

                case "Сложный":
                    pool = lowerCase + upperCase + digits + symbols;
                    requiredGroups = new[] { lowerCase, upperCase, digits, symbols };
                    break;

                default:
                    pool = lowerCase + upperCase + digits;
                    requiredGroups = new[] { lowerCase, upperCase, digits };
                    break;
            }

            List<char> characters = new();

            foreach (string group in requiredGroups)
            {
                characters.Add(group[RandomNumberGenerator.GetInt32(group.Length)]);
            }

            while (characters.Count < PasswordLength)
            {
                characters.Add(pool[RandomNumberGenerator.GetInt32(pool.Length)]);
            }

            for (int i = characters.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (characters[i], characters[j]) = (characters[j], characters[i]);
            }

            return new string(characters.ToArray());
        }
    }
}