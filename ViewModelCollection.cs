using System;
using System.Net;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections.Specialized;


namespace Shindr.CSharpLib
{

  #region ViewModelWrapper

  /// <summary>
  /// Generic viewmodel wrapper arround model
  /// </summary>
  public interface IViewModelWrapper<T>
  {
    /// <summary>
    /// Gets/Sets underlying model
    /// </summary>
    T Model { get; set; }
  }

  #endregion



  /// <summary>
  /// Generic viewmodel wrapper for model collections/lists
  /// </summary>
  public class ViewModelCollection<TViewModel, TModel> : ObservableCollection<TViewModel>, IViewModelWrapper<IList<TModel>>
    where TViewModel : class, IViewModelWrapper<TModel>, new()
  {
    private object _holdLock = new object();
    private bool _ignoreUpdates = false;

    private int _recycleIndex = 0;
    private TViewModel[] _recycledViewModels = new TViewModel[8];


    /// <summary>
    /// Creates new instance ViewModelCollection
    /// </summary>
    public ViewModelCollection()
    {
      RecycleViewModels = true;
    }


    /// <summary>
    /// Creates new instance ViewModelCollection wrapped around modelList 
    /// </summary>
    /// <param name="modelCollection">modelList to wrap arround</param>
    public ViewModelCollection(IList<TModel> modelCollection)
      : this(modelCollection, null)
    { }


    /// <summary>
    /// Creates new instance ViewModelCollection wrapped around modelList 
    /// </summary>
    /// <param name="modelCollection">modelList to wrap arround</param>
    /// <param name="viewModelInitAction">Custom action used to initialize viewmodels after instantiation</param>
    public ViewModelCollection(IList<TModel> modelCollection, Action<TViewModel> viewModelInitAction)
      : this()
    {
      this.CollectionChanged += new NotifyCollectionChangedEventHandler(ViewModel_CollectionChanged);
      ViewModelInit = viewModelInitAction;

      if (modelCollection == null)
        Model = new ObservableCollection<TModel>();
      else
        Model = modelCollection;
    }


    /// <summary>
    /// Gets/Sets underlying Model
    /// </summary>
    public IList<TModel> Model
    {
      get { return _model; }
      set
      {
        if (_model is INotifyCollectionChanged)//unregister old model
          ((INotifyCollectionChanged)_model).CollectionChanged -= new NotifyCollectionChangedEventHandler(Model_CollectionChanged);

        _model = value;

        if (_model is INotifyCollectionChanged)//register new model
          ((INotifyCollectionChanged)_model).CollectionChanged += new NotifyCollectionChangedEventHandler(Model_CollectionChanged);

        Model_CollectionChanged(_model, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
      }
    }
    IList<TModel> _model;


    /// <summary>
    /// Action for custom initiliazation of viewmodel
    /// </summary>
    public Action<TViewModel> ViewModelInit { get; set; }


    /// <summary>
    /// Get/Sets if viewmodels should be recycled (usefull when moving models by first removing and then adding)
    /// </summary>
    public bool RecycleViewModels { get; set; }



    /// <summary>
    /// Synchronizes chages from the ViewModels collection to the Models collection
    /// </summary>
    void ViewModel_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      //recycle removed viewmodel
      if (e.Action == NotifyCollectionChangedAction.Remove && RecycleViewModels)
      {
        foreach (object vm in e.OldItems)
          RecycleViewModel(vm as TViewModel);
      }


      if (_ignoreUpdates) return;

      _ignoreUpdates = true;
      try
      {
        UpdateCollection(Model, this.Items, e, vm => vm.Model);
      }
      finally
      {
        _ignoreUpdates = false;
      }
    }


    /// <summary>
    /// Synchronizes chages from the Models collection to the ViewModels collection
    /// </summary>
    void Model_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {

      if (_ignoreUpdates) return;

      _ignoreUpdates = true;
      try
      {
        UpdateCollection(this, Model, e, ViewModelFactory);
      }
      finally
      {
        _ignoreUpdates = false;
      }
    }


    //factory method for creating viewmodels from models
    TViewModel ViewModelFactory(TModel model)
    {
      TViewModel result = null;

      if (RecycleViewModels)
        result = TryReuseViewModel(model);
      if (result == null)
      {
        result = new TViewModel() { Model = model };
        if (ViewModelInit != null)
          ViewModelInit(result);
      }

      return result;
    }


    private void UpdateCollection<T, U>(IList<T> targetCollection, IList<U> sourceCollection, NotifyCollectionChangedEventArgs e, Func<U, T> itemFactory)
    {
      switch (e.Action)
      {
        case NotifyCollectionChangedAction.Reset:
          targetCollection.Clear();
          foreach (U srcItem in sourceCollection)
          {
            T item = itemFactory(srcItem);
            targetCollection.Add(item);
          }
          break;

        case NotifyCollectionChangedAction.Add:
          int i = e.NewStartingIndex;
          foreach (U srcItem in e.NewItems)
          {
            T item = itemFactory(srcItem);
            targetCollection.Insert(i, item);
            i++;
          }
          break;

        case NotifyCollectionChangedAction.Remove:
          int remIdx = e.OldStartingIndex;
          foreach (U srcItem in e.OldItems)
          {
            targetCollection.RemoveAt(remIdx);
            remIdx++;
          }
          break;

        case NotifyCollectionChangedAction.Replace:
          int idx = e.OldStartingIndex;
          foreach (U srcItem in e.NewItems)
          {
            T item = itemFactory(srcItem);
            targetCollection[idx] = item;
            idx++;
          }
          break;
      }
    }


    /// <summary>
    /// Adds viewmodel to recycle list
    /// </summary>
    private void RecycleViewModel(TViewModel viewModel)
    {
      if ((viewModel == null) || _recycledViewModels.Any(vm => vm == viewModel)) return;

      _recycledViewModels[_recycleIndex] = viewModel;
      _recycleIndex = (_recycleIndex + 1) % _recycledViewModels.Length;
    }


    /// <summary>
    /// Tries to find viewmodel for a model from recycled list
    /// </summary>
    private TViewModel TryReuseViewModel(TModel model)
    {
      return _recycledViewModels.Where(vm => vm != null).FirstOrDefault(vm => object.Equals(vm.Model, model));
    }

  }
}
