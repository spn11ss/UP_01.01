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
    /// Логика взаимодействия для ComplaintWindow.xaml
    /// </summary>
    /// <remarks>
    /// Универсальное окно для подачи жалобы на контент.
    /// Позволяет пользователю отправить жалобу на:
    /// - Книгу (вызывается из BookWindow)
    /// - Автора (вызывается из BookWindow)
    /// - Отзыв (вызывается из BookWindow)
    /// 
    /// Результат работы:
    /// - IsSubmitted = true - жалоба была отправлена (пользователь нажал "Отправить")
    /// - IsSubmitted = false - жалоба не была отправлена (пользователь нажал "Отмена")
    /// - Reason - текст причины жалобы (заполняется только при IsSubmitted = true)
    /// 
    /// После закрытия окна вызывающая сторона (BookWindow) создаёт запись в таблице Complaints
    /// с указанием типа жалобы (BookId, ReviewId или TargetUserId) и причиной.
    /// </remarks>
    public partial class ComplaintWindow : Window
    {
        /// <summary>
        /// Текст причины жалобы, указанный пользователем
        /// </summary>
        /// <remarks>
        /// Доступен только после нажатия кнопки "Отправить" (IsSubmitted = true).
        /// Используется вызывающей стороной (BookWindow) для создания записи в таблице Complaints.
        /// </remarks>
        public string Reason { get; private set; }

        /// <summary>
        /// Флаг, указывающий, была ли жалоба отправлена
        /// </summary>
        /// <remarks>
        /// true - пользователь заполнил причину и нажал "Отправить"
        /// false - пользователь нажал "Отмена" или закрыл окно
        /// </remarks>
        public bool IsSubmitted { get; private set; } = false;

        /// <summary>
        /// Конструктор окна подачи жалобы
        /// </summary>
        /// <param name="title">Заголовок окна (определяет тип жалобы)</param>
        /// <remarks>
        /// Выполняет:
        /// 1. InitializeComponent() - загрузка XAML разметки окна
        /// 2. Устанавливает переданный заголовок в TitleText
        /// 
        /// Примеры вызова из BookWindow:
        /// - new ComplaintWindow("Жалоба на книгу") - жалоба на книгу
        /// - new ComplaintWindow("Жалоба на автора") - жалоба на автора
        /// - new ComplaintWindow("Жалоба на отзыв") - жалоба на отзыв
        /// 
        /// Заголовок отображается пользователю и помогает понять, на что именно он жалуется.
        /// </remarks>
        public ComplaintWindow(string title)
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
        /// и, если true, создаёт жалобу в базе данных с указанной причиной.
        /// </remarks>
        private void SubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ReasonBox.Text))
            {
                MessageBox.Show("Пожалуйста, укажите причину жалобы.",
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
        /// IsSubmitted остаётся false, вызывающая сторона не создаёт жалобу.
        /// </remarks>
        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}