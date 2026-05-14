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
    /// Логика взаимодействия для AppealWindow.xaml
    /// </summary>
    /// <remarks>
    /// Универсальное окно для подачи заявки на разморозку (аккаунта или книги).
    /// Предоставляет пользователю возможность указать причину для оспаривания заморозки.
    /// 
    /// Используется в двух контекстах:
    /// - ProfilePage: оспаривание заморозки аккаунта
    /// - AuthorPage: оспаривание заморозки книги
    /// 
    /// Результат работы:
    /// - IsSubmitted = true - заявка была отправлена (пользователь нажал "Отправить")
    /// - IsSubmitted = false - заявка не была отправлена (пользователь нажал "Отмена")
    /// - Reason - текст причины (заполняется только при IsSubmitted = true)
    /// </remarks>
    public partial class AppealWindow : Window
    {
        /// <summary>
        /// Текст причины, указанный пользователем
        /// </summary>
        /// <remarks>
        /// Доступен только после нажатия кнопки "Отправить" (IsSubmitted = true).
        /// Используется вызывающей стороной (ProfilePage или AuthorPage) для создания
        /// записи в таблице UnfreezeApplications.
        /// </remarks>
        public string Reason { get; private set; }

        /// <summary>
        /// Флаг, указывающий, была ли заявка отправлена
        /// </summary>
        /// <remarks>
        /// true - пользователь заполнил причину и нажал "Отправить"
        /// false - пользователь нажал "Отмена" или закрыл окно
        /// </remarks>
        public bool IsSubmitted { get; private set; } = false;

        /// <summary>
        /// Конструктор окна оспаривания заморозки
        /// </summary>
        /// <param name="title">Заголовок окна (по умолчанию "Оспаривание заморозки")</param>
        /// <remarks>
        /// Выполняет:
        /// 1. InitializeComponent() - загрузка XAML разметки окна
        /// 2. Устанавливает переданный заголовок в TitleText
        /// 
        /// Примеры вызова:
        /// - new AppealWindow() - заголовок по умолчанию
        /// - new AppealWindow("Оспаривание заморозки аккаунта") - для профиля
        /// - new AppealWindow("Оспаривание заморозки книги") - для страницы автора
        /// </remarks>
        public AppealWindow(string title = "Оспаривание заморозки")
        {
            InitializeComponent();
            TitleText.Text = title;
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Отправить"
        /// </summary>
        /// <param name="sender">Кнопка SubmitBtn</param>
        /// <param name="e">Параметры события нажатия кнопки</param>
        /// <remarks>
        /// Алгоритм:
        /// 1. Проверяет, что поле причины не пустое
        /// 2. Если причина не указана - показывает предупреждение и прерывает отправку
        /// 3. Если причина указана - сохраняет её в свойство Reason
        /// 4. Устанавливает флаг IsSubmitted = true
        /// 5. Закрывает окно
        /// 
        /// Важно: после закрытия окна вызывающая сторона проверяет IsSubmitted
        /// и, если true, создаёт заявку в базе данных с указанной причиной.
        /// </remarks>
        private void SubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ReasonBox.Text))
            {
                MessageBox.Show("Пожалуйста, укажите причину для разморозки.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Reason = ReasonBox.Text.Trim();
            IsSubmitted = true;
            this.Close();
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Отмена"
        /// </summary>
        /// <param name="sender">Кнопка CancelBtn</param>
        /// <param name="e">Параметры события нажатия кнопки</param>
        /// <remarks>
        /// Просто закрывает окно без сохранения причины.
        /// IsSubmitted остаётся false, вызывающая сторона не создаёт заявку.
        /// </remarks>
        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}