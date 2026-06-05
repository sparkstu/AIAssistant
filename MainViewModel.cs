using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WpfDemo
{
    partial class MainViewModel : ObservableObject
    {
        // 🔥 自动生成绑定属性：Title
        [ObservableProperty]
        private string _title = "MVVM 框架已成功运行！";

        // 🔥 自动生成绑定属性：InputText
        [ObservableProperty]
        private string _inputText;

        // 🔥 自动生成命令：ShowMessageCommand
        [RelayCommand]
        private void ShowMessage()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "视频文件|*.mp4;*.avi;*.wmv;*.mkv|所有文件|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _inputText = openFileDialog.FileName;
            }
        }
    }
}
