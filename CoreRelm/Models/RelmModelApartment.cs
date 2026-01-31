using Newtonsoft.Json;
using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models
{
    /// <summary>
    /// Represents an apartment entity within the Relm data model, including user and member association information.
    /// </summary>
    /// <remarks>This class provides properties for identifying the apartment and its associated user and
    /// member. It is typically used in scenarios where apartment membership and user linkage are required within the
    /// Relm system. Inherits from RelmModel and implements IRelmModelApartment.</remarks>
    public class RelmModelApartment : RelmModel, IRelmModelApartment
    {
        /// <summary>
        /// Initializes a new instance of the RelmModelApartment class.
        /// </summary>
        public RelmModelApartment() : base() { }

        /// <summary>
        /// Initializes a new instance of the RelmModelApartment class using the specified data row and optional
        /// alternate table name.
        /// </summary>
        /// <param name="ModelData">The DataRow containing the data to initialize the apartment model. Cannot be null.</param>
        /// <param name="AlternateTableName">An optional table name to use instead of the default. If null, the class name is used.</param>
        public RelmModelApartment(DataRow ModelData, string AlternateTableName = null) : base(ModelData, AlternateTableName ?? nameof(RelmModelApartment)) { }

        /// <summary>
        /// Initializes a new instance of the RelmModelApartment class by copying data from the specified model.
        /// </summary>
        /// <param name="fromModel">The source model from which to copy data. Cannot be null.</param>
        public RelmModelApartment(IRelmModel fromModel) : base(fromModel) { }

        /// <summary>
        /// Gets or sets the unique identifier for the apartment.
        /// </summary>
        [RelmColumn]
        public string ApartmentId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        [RelmColumn]
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the email address associated with the user.
        /// </summary>
        [RelmColumn]
        public string UserEmail { get; set; }

        /// <summary>
        /// Gets or sets the user name associated with the entity.
        /// </summary>
        [RelmColumn]
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the associated Relm member for this entity.
        /// </summary>
        [RelmColumn]
        public IRelmMember Member { get; set; }

        /*
        private long _createUserId;
        private long _lastUpdatedUserId;
        private IMember _createUser;
        private IMember _lastUpdatedUser;

        [DALResolvable]
        public long LinkedCompanyId { get; set; }

        [DALResolvable]
        public long CreateUserId
        {
            get
            {
                return _createUserId;
            }
            set
            {
                _createUserId = value;

                if (_createUserId != _createUser?.Id)
                {
                    // force all reload
                    _createUser = null;
                    _ = CreateUser;
                }
            }
        }

        [DALResolvable]
        public long LastUpdatedUserId
        {
            get
            {
                return _lastUpdatedUserId;
            }
            set
            {
                _lastUpdatedUserId = value;

                if (_lastUpdatedUserId != _lastUpdatedUser?.Id)
                {
                    // force all reload
                    _lastUpdatedUser = null;
                    _ = LastUpdatedUser;
                }
            }
        }

        [JsonIgnore]
        public IMember CreateUser
        {
            get
            {
                if (_createUser == null) // singleton
                {
                    // force all reload
                    _createUser = MembershipHelper.GetMemberByUserId((int)_createUserId);
                }

                return _createUser;
            }
            set
            {
                _createUser = value;
                _createUserId = _createUser?.Id ?? default;
            }
        }

        [JsonIgnore]
        public IMember LastUpdatedUser
        {
            get
            {
                if (_lastUpdatedUser == null) // singleton
                {
                    // force all reload
                    _lastUpdatedUser = MembershipHelper.GetMemberByUserId((int)_lastUpdatedUserId);
                }

                return _lastUpdatedUser;
            }
            set
            {
                _lastUpdatedUser = value;
                _lastUpdatedUserId = _lastUpdatedUser?.Id ?? default;
            }
        }

        public CS_ApartmentedData SetCurrentMemberApartment(bool UpdateCreate = true)
        {
            var currentMemberId = MembershipHelper.GetCurrentMember()?.Id ?? 0;

            return SetMemberApartment(currentMemberId, UpdateCreate);
        }

        public CS_ApartmentedData SetMemberApartment(int MemberId, bool UpdateCreate = true)
        {
            if (UpdateCreate)
                CreateUserId = MemberId; // reload all data

            LastUpdatedUserId = MemberId; // reload all data
            return this;
        }
        */
        /// <summary>
        /// Sets the apartment membership information for the specified member and updates tracking fields as specified.
        /// </summary>
        /// <param name="MemberId">The identifier of the member whose apartment information is to be set.</param>
        /// <param name="UpdateCreate">true to update the creation tracking fields in addition to the last updated fields; otherwise, false. The
        /// default is true.</param>
        /// <returns>The current instance with updated membership information.</returns>
        public IRelmModelApartment SetMemberApartment(int MemberId, bool UpdateCreate = true)
        {
            if (UpdateCreate)
                UserId = MemberId; // reload all data

            UserId = MemberId; // reload all data
            return this;
        }
    }
}
