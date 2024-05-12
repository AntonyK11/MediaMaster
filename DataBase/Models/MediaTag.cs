using System.ComponentModel.DataAnnotations;

namespace MediaMaster.DataBase.Models;

public class MediaTag
{
    public virtual int MediaId { get; set; }
    public virtual int TagId { get; set; }
}