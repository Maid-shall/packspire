using System;
using UnityEngine.UIElements;

namespace Packspire {
/// <summary>Shared retained-mode controls. Screen code should use these instead of one-off styling.</summary>
public static class PackspireUiFactory {
 public static Label Title(string text){var label=new Label(text);label.AddToClassList("ps-title");return label;}
 public static Label Body(string text){var label=new Label(text);label.AddToClassList("ps-body");return label;}
 public static Button Button(string text,Action clicked){var button=new Button(clicked){text=text};button.AddToClassList("ps-button");return button;}
 public static VisualElement Card(string title,string description,Action clicked=null){var card=new VisualElement();card.AddToClassList("ps-card");card.Add(Title(title));card.Add(Body(description));if(clicked!=null){card.focusable=true;card.RegisterCallback<ClickEvent>(_=>clicked());}return card;}
 public static VisualElement EmptyState(string title,string description){var state=new VisualElement();state.AddToClassList("ps-card");state.AddToClassList("ps-empty-state");state.Add(Title(title));state.Add(Body(description));return state;}
 public static VisualElement Dialog(string title,string description,string confirmLabel,Action confirm,string cancelLabel,Action cancel){var dialog=new VisualElement();dialog.AddToClassList("ps-dialog");dialog.Add(Title(title));dialog.Add(Body(description));var actions=new VisualElement();actions.AddToClassList("ps-dialog-actions");actions.Add(Button(cancelLabel,cancel));actions.Add(Button(confirmLabel,confirm));dialog.Add(actions);return dialog;}
}
}
