using WebAPI_DAV.Models.Domain;

namespace WebAPI_DAV.Repositories
{
    public interface IImageRepository
    {
        Image Upload(Image image);
        List<Image> GetAllInfoImages();
        List<Image> GetImagesByRoomId(int roomId);
        (byte[], string, string) DownloadFile(int Id);
    }
}