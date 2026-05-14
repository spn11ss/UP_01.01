using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UP
{
    /// <summary>
    /// Логика взаимодействия для AuthWindow.xaml
    /// </summary>
    /// <remarks>
    /// Класс управляет окном авторизации и регистрации.
    /// Содержит два режима: вход в систему (по умолчанию) и регистрация нового пользователя.
    /// При успешном входе сохраняет пользователя в Session.CurrentUser и открывает MainWindow.
    /// При регистрации создаёт нового пользователя в базе данных с ролью "Читатель" (RoleId = 1).
    /// Все поля ввода проходят валидацию перед отправкой в базу данных.
    /// </remarks>
    public partial class AuthWindow : Window
    {
        /// <summary>
        /// Флаг текущего режима окна: true - режим входа, false - режим регистрации
        /// </summary>
        private bool isLoginMode = true;

        /// <summary>
        /// Конструктор окна авторизации
        /// </summary>
        /// <remarks>
        /// Инициализирует XAML компоненты и устанавливает режим входа по умолчанию.
        /// </remarks>
        public AuthWindow()
        {
            InitializeComponent();
            SetLoginMode();
        }

        /// <summary>
        /// Переключает окно в режим входа в систему
        /// </summary>
        /// <remarks>
        /// Выполняет следующие действия:
        /// 1. Устанавливает флаг isLoginMode = true
        /// 2. Меняет заголовок окна на "ВХОД В СИСТЕМУ"
        /// 3. Меняет текст кнопки на "ВОЙТИ"
        /// 4. Меняет текст ссылки переключения на "Зарегистрироваться"
        /// 5. Показывает панель с полями для входа (LoginPanel)
        /// 6. Скрывает панель с полями для регистрации (RegisterPanel)
        /// 7. Очищает все поля ввода и сообщение об ошибке
        /// </remarks>
        private void SetLoginMode()
        {
            isLoginMode = true;
            TitleText.Text = "ВХОД В СИСТЕМУ";
            ActionButton.Content = "ВОЙТИ";
            ToggleLink.Text = " Зарегистрироваться";

            // Показываем панель входа
            LoginPanel.Visibility = Visibility.Visible;
            RegisterPanel.Visibility = Visibility.Collapsed;

            // Очищаем поля
            LoginBox.Text = "";
            PasswordBox.Password = "";
            ErrorText.Text = "";
        }

        /// <summary>
        /// Переключает окно в режим регистрации нового пользователя
        /// </summary>
        /// <remarks>
        /// Выполняет следующие действия:
        /// 1. Устанавливает флаг isLoginMode = false
        /// 2. Меняет заголовок окна на "РЕГИСТРАЦИЯ"
        /// 3. Меняет текст кнопки на "ЗАРЕГИСТРИРОВАТЬСЯ"
        /// 4. Меняет текст ссылки переключения на "Войти"
        /// 5. Показывает панель с полями для регистрации (RegisterPanel)
        /// 6. Скрывает панель с полями для входа (LoginPanel)
        /// 7. Очищает все поля ввода и сообщение об ошибке
        /// </remarks>
        private void SetRegisterMode()
        {
            isLoginMode = false;
            TitleText.Text = "РЕГИСТРАЦИЯ";
            ActionButton.Content = "ЗАРЕГИСТРИРОВАТЬСЯ";
            ToggleLink.Text = " Войти";

            // Показываем панель регистрации
            LoginPanel.Visibility = Visibility.Collapsed;
            RegisterPanel.Visibility = Visibility.Visible;

            // Очищаем поля
            RegLoginBox.Text = "";
            EmailBox.Text = "";
            UserNameBox.Text = "";
            RegPasswordBox.Password = "";
            ConfirmPasswordBox.Password = "";
            ErrorText.Text = "";
        }

        /// <summary>
        /// Обработчик клика по ссылке переключения между режимами
        /// </summary>
        /// <remarks>
        /// При клике проверяет текущий режим:
        /// - Если режим входа - переключает на регистрацию
        /// - Если режим регистрации - переключает на вход
        /// </remarks>
        /// <param name="sender">Источник события (TextBlock ToggleText)</param>
        /// <param name="e">Параметры события мыши</param>
        private void ToggleText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isLoginMode)
                SetRegisterMode();
            else
                SetLoginMode();
        }

        /// <summary>
        /// Обработчик нажатия на главную кнопку действия
        /// </summary>
        /// <remarks>
        /// В зависимости от текущего режима вызывает метод Login() или Register()
        /// </remarks>
        /// <param name="sender">Источник события (Button ActionButton)</param>
        /// <param name="e">Параметры события нажатия кнопки</param>
        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (isLoginMode)
                Login();
            else
                Register();
        }

        /// <summary>
        /// Выполняет авторизацию пользователя в системе
        /// </summary>
        /// <remarks>
        /// Алгоритм входа:
        /// 1. Проверяет, что поля логин и пароль не пустые
        /// 2. Ищет в базе данных пользователя с указанным логином и паролем
        /// 3. Если пользователь не найден - выводит сообщение об ошибке
        /// 4. Если пользователь найден - сохраняет его в Session.CurrentUser
        /// 5. Если аккаунт заморожен (IsFrozen == true) - показывает предупреждение с причиной
        /// 6. Открывает главное окно MainWindow
        /// 7. Закрывает окно авторизации
        /// </remarks>
        private void Login()
        {
            string login = LoginBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(login))
            {
                ErrorText.Text = "Введите логин";
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ErrorText.Text = "Введите пароль";
                return;
            }

            var user = Core.Context.Users.FirstOrDefault(u => u.Login == login && u.Password == password);

            if (user == null)
            {
                ErrorText.Text = "Неверный логин или пароль";
                return;
            }

            Session.CurrentUser = user;

            if (user.IsFrozen == true)
            {
                MessageBox.Show($"Ваш аккаунт заморожен!\nПричина: {user.FreezeReason ?? "Не указана"}\n\nВы можете подать заявку на разморозку в профиле.",
                    "Аккаунт заморожен", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        /// <summary>
        /// Выполняет регистрацию нового пользователя
        /// </summary>
        /// <remarks>
        /// Алгоритм регистрации:
        /// 1. Собирает данные из всех полей регистрации
        /// 2. Проверяет, что все поля заполнены
        /// 3. Проверяет длину логина (минимум 3 символа)
        /// 4. Проверяет корректность формата email (наличие символов @ и .)
        /// 5. Проверяет длину пароля (минимум 5 символов)
        /// 6. Проверяет совпадение пароля и подтверждения
        /// 7. Проверяет уникальность логина в базе данных
        /// 8. Проверяет уникальность email в базе данных
        /// 9. Проверяет уникальность имени пользователя в базе данных
        /// 10. Создаёт нового пользователя с ролью "Читатель" (RoleId = 1)
        /// 11. Сохраняет пользователя в базу данных
        /// 12. Показывает сообщение об успешной регистрации
        /// 13. Переключается в режим входа и автоматически подставляет зарегистрированный логин
        /// </remarks>
        private void Register()
        {
            string login = RegLoginBox.Text.Trim();
            string email = EmailBox.Text.Trim();
            string userName = UserNameBox.Text.Trim();
            string password = RegPasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            // Проверки
            if (string.IsNullOrEmpty(login))
            {
                ErrorText.Text = "Введите логин";
                return;
            }

            if (login.Length < 3)
            {
                ErrorText.Text = "Логин должен быть не менее 3 символов";
                return;
            }

            if (string.IsNullOrEmpty(email))
            {
                ErrorText.Text = "Введите email";
                return;
            }

            if (!email.Contains("@") || !email.Contains("."))
            {
                ErrorText.Text = "Введите корректный email (пример: name@mail.ru)";
                return;
            }

            if (string.IsNullOrEmpty(userName))
            {
                ErrorText.Text = "Введите имя пользователя";
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ErrorText.Text = "Введите пароль";
                return;
            }

            if (password.Length < 5)
            {
                ErrorText.Text = "Пароль должен быть не менее 5 символов";
                return;
            }

            if (password != confirmPassword)
            {
                ErrorText.Text = "Пароли не совпадают";
                return;
            }

            if (Core.Context.Users.Any(u => u.Login == login))
            {
                ErrorText.Text = "Логин уже занят";
                return;
            }

            if (Core.Context.Users.Any(u => u.Email == email))
            {
                ErrorText.Text = "Email уже занят";
                return;
            }

            if (Core.Context.Users.Any(u => u.UserName == userName))
            {
                ErrorText.Text = "Имя пользователя уже занято";
                return;
            }

            var newUser = new Users
            {
                Login = login,
                Password = password,
                Email = email,
                UserName = userName,
                RoleId = 1,
                IsFrozen = false,
                DateRegistration = DateTime.Now
            };

            Core.Context.Users.Add(newUser);
            Core.Context.SaveChanges();

            MessageBox.Show("Регистрация успешна! Теперь войдите в систему.", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);

            SetLoginMode();
            LoginBox.Text = login;
        }
    }
}
