using System.Threading.Tasks;

public interface ISceneTransition
{
    Task ShowAsync();

    Task HideAsync();

    void SetProgress(float normalizedProgress);
}
