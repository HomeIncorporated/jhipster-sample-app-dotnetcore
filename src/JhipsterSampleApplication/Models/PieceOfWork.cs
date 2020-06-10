using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MyCompany.Models.RelationshipTools;
using Newtonsoft.Json;

namespace MyCompany.Models {
    [Table("piece_of_work")]
    public class PieceOfWork {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        [JsonIgnore]
        public IList<JobChore> JobChores { get; } = new List<JobChore>();

        [NotMapped]
        [JsonIgnore]
        public IList<Job> Jobs { get; }

        // jhipster-needle-entity-add-field - JHipster will add fields here, do not remove

        public PieceOfWork()
        {
            Jobs = new JoinListFacade<Job, PieceOfWork, JobChore>(this, JobChores);
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (obj == null || GetType() != obj.GetType()) return false;
            var pieceOfWork = obj as PieceOfWork;
            if (pieceOfWork?.Id == null || pieceOfWork?.Id == 0 || Id == 0) return false;
            return EqualityComparer<long>.Default.Equals(Id, pieceOfWork.Id);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        public override string ToString()
        {
            return "PieceOfWork{" +
                    $"ID='{Id}'" +
                    $", Title='{Title}'" +
                    $", Description='{Description}'" +
                    "}";
        }
    }
}
