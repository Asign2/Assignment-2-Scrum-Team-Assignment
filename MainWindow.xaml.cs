using System;
using System.Windows;
using System.Windows.Controls;

namespace Assign_2
{
    public partial class MainWindow : Window
    {
        private UserRecord selectedUser;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                SensorsDatabase.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Database error:\n\n" + ex.Message);
            }
        }
        /// <summary>
        /// Compares users account input to the database account credentials.
        /// If matches then sends user to their account role's screen.
        /// </summary>
        /// <param name="sender">The login button that was Clicked</param>
        /// <param name="e">The event data.</param>
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;
            string role;

            try
            {
                bool success = UserDatabase.Login(username, password, out role);

                if (success == false)
                {
                    MessageBox.Show("Invalid username or password.");
                    return;
                }

                if (role == "Admin")
                {
                    OpenAdminScreen();
                }
                else
                {
                    ShowScreen(UserScreen);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        /// <summary>
        /// Redundant method
        /// change all  OpenAdminScreen(); to just ShowScreen(AdminScreen);
        /// </summary>
        private void OpenAdminScreen()
        {
            ShowScreen(AdminScreen);
            RefreshUserList();
        }

        /// <summary>
        /// Refreshes the user grid when it is updated/changed.
        /// </summary>
        private void RefreshUserList()
        {
            UsersGrid.ItemsSource = UserDatabase.GetAllUsers();
        }

        /// <summary>
        /// Grabs the values from the box clicked to input it into the edit account input boxes.
        /// </summary>
        /// <param name="sender">The box on the grid that was Clicked</param>
        /// <param name="e">The event data.</param>
        private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedUser = UsersGrid.SelectedItem as UserRecord;

            if (selectedUser == null)
            {
                return;
            }

            EditUsernameBox.Text = selectedUser.Username;
            NewPasswordBox.Password = "";

            foreach (ComboBoxItem item in RoleBox.Items)
            {
                if (item.Content.ToString() == selectedUser.Role)
                {
                    RoleBox.SelectedItem = item;
                }
            }
        }

        /// <summary>
        /// Commit the changes that were in the edit/update account input boxes.
        /// </summary>
        /// <param name="sender">The save changes button that was Clicked</param>
        /// <param name="e">The event data.</param>
        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null)
            {
                MessageBox.Show("Select a user first.");
                return;
            }

            string newUsername = EditUsernameBox.Text.Trim();
            ComboBoxItem chosenRole = RoleBox.SelectedItem as ComboBoxItem;

            if (newUsername == "" || chosenRole == null)
            {
                MessageBox.Show("Username and role are required.");
                return;
            }

            string newRole = chosenRole.Content.ToString();

            try
            {
                UserDatabase.UpdateUser(selectedUser.Id, newUsername, newRole);
                RefreshUserList();
                MessageBox.Show("User updated.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        /// <summary>
        /// Updates the database with the password from the input box.
        /// </summary>
        /// <param name="sender">The reset password button that was Clicked</param>
        /// <param name="e">The event data.</param>
        private void ResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null)
            {
                MessageBox.Show("Select a user first.");
                return;
            }

            if (NewPasswordBox.Password == "")
            {
                MessageBox.Show("Enter a new password.");
                return;
            }

            UserDatabase.UpdatePassword(selectedUser.Id, NewPasswordBox.Password);
            NewPasswordBox.Password = "";
            MessageBox.Show("Password updated.");
        }

        /// <summary>
        /// Deletes the selected user from the database. 
        /// </summary>
        /// <param name="sender">The delete user button that was Clicked</param>
        /// <param name="e">The event data.</param>
        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null)
            {
                MessageBox.Show("Select a user first.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Delete user '" + selectedUser.Username + "'?",
                "Confirm Delete",
                MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                UserDatabase.DeleteUser(selectedUser.Id);
                RefreshUserList();
            }
        }

        /// <summary>
        /// Swaps the Display to the new user registration screen. 
        /// </summary>
        /// <param name="sender">The show new user button that was Clicked</param>
        /// <param name="e">The event data.</param>
        private void ShowNewUserScreen_Click(object sender, RoutedEventArgs e)
        {
            ShowScreen(NewUserScreen);
        }
        /// <summary>
        /// Registers a new user to the database with the inputted values from the new user registration screen.
        /// </summary>
        /// <param name="sender">The register new user button that was Clicked</param>
        /// <param name="e">The event data.</param>
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = NewUsername.Text.Trim();
            string password = NewPassword.Password;
            ComboBoxItem chosenRole = NewRole.SelectedItem as ComboBoxItem;

            if (username == "" || password == "" || chosenRole == null)
            {
                MessageBox.Show("All fields are required.");
                return;
            }

            string role = chosenRole.Content.ToString();

            try
            {
                UserDatabase.Register(username, password, role);
                MessageBox.Show("User registered.");
                NewUsername.Clear();
                NewPassword.Clear();
                NewRole.SelectedItem = null;
                OpenAdminScreen();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        /// <summary>
        /// Returns the display to the admin screen from the new user registration screen.
        /// </summary>
        /// <param name="sender">The retunr button that was Clicked</param>
        /// <param name="e">The event data.</param>
        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            OpenAdminScreen();
        }
        /// <summary>
        /// Returns the display to the login screen from any other screen and clears the input boxes.
        /// </summary>
        /// <param name="sender">The logout button that was Clicked</param>
        /// <param name="e">The event data.</param>
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            UsernameBox.Text = "";
            PasswordBox.Password = "";
            ShowScreen(LoginScreen);
        }

        /// <summary>
        /// Handles the visibility of the screens, hiding all other screens and showing the one passed in.
        /// </summary>
        /// <param name="screenToShow">The screen that will be shown.</param>
        private void ShowScreen(UIElement screenToShow)
        {
            LoginScreen.Visibility = Visibility.Collapsed;
            AdminScreen.Visibility = Visibility.Collapsed;
            NewUserScreen.Visibility = Visibility.Collapsed;
            UserScreen.Visibility = Visibility.Collapsed;

            screenToShow.Visibility = Visibility.Visible;
        }
    }
}