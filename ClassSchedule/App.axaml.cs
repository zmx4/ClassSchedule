using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using ClassSchedule.Data;
using ClassSchedule.Services;
using ClassSchedule.ViewModels;
using ClassSchedule.Views;
using Microsoft.EntityFrameworkCore;

namespace ClassSchedule;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = CreateMainViewModel()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = CreateMainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>创建主页面 ViewModel，从 SQLite 加载课表数据。</summary>
    private static MainViewModel CreateMainViewModel()
    {
        try
        {
            var dbPath = Path.Combine(AppContext.BaseDirectory, "classschedule.db");
            var options = new DbContextOptionsBuilder<ClassScheduleDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;
            var db = new ClassScheduleDbContext(options);
            db.Database.EnsureCreated();
            return new MainViewModel(new ScheduleRepository(db));
        }
        catch (Exception ex)
        {
            var viewModel = new MainViewModel();
            viewModel.StatusMessage = "打开数据库失败：" + ex.Message;
            return viewModel;
        }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // // Get an array of plugins to remove
        // var dataValidationPluginsToRemove =
        //     BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();
        //
        // // remove each entry found
        // foreach (var plugin in dataValidationPluginsToRemove)
        // {
        //     BindingPlugins.DataValidators.Remove(plugin);
        // }
    }
}