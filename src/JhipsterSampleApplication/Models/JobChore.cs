using MyCompany.Models.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyCompany.Models {
    [Table("job_chore")]
    public class JobChore : IJoinedEntity<Job>, IJoinedEntity<PieceOfWork> {
        public long JobId { get; set; }
        public Job Job { get; set; }
        Job IJoinedEntity<Job>.Join {
            get => Job;
            set => Job = value;
        }

        public long PieceOfWorkId { get; set; }
        public PieceOfWork PieceOfWork { get; set; }
        PieceOfWork IJoinedEntity<PieceOfWork>.Join {
            get => PieceOfWork;
            set => PieceOfWork = value;
        }
    }
}
