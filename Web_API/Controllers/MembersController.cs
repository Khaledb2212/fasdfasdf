using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_API.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Web_API.Controllers
{
    //[Route("api/[controller]")]
    [Route("api/Members")]
    [ApiController]
    public class MembersController : ControllerBase
    {
        private readonly ProjectDbContext _context;

        public MembersController(ProjectDbContext context)
        {
            _context = context;
        }

        //https://localhost:7085/api/Members/GetMembers
        [HttpGet("GetMembers", Name = "GetMembers")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<Member>>> GetMembers()
        {
            try
            {
                // Including 'person' so we see the Member's name, not just ID
                var members = await _context.Members
                                            .Include(m => m.person)
                                            .ToListAsync();

                if (members == null || members.Count == 0)
                {
                    return NotFound("No members found.");
                }

                return Ok(members);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving members: " + ex.Message);
            }
        }

        //https://localhost:7085/api/Members/GetMember?id=5
        [HttpGet("GetMember", Name = "GetMember")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Member>> GetMember(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Invalid ID");
                }

                var member = await _context.Members
                                           .Include(m => m.person) // Include personal details
                                           .FirstOrDefaultAsync(m => m.MemberID == id);

                if (member == null)
                {
                    return NotFound($"Member with ID {id} not found.");
                }

                return Ok(member);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving member: " + ex.Message);
            }
        }


        //https://localhost:7085/api/Members/PostMember
        [HttpPost("PostMember",Name = "PostMember")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Member>> PostMember(Member member)
        {
            // 1. Basic Validation
            if (member == null)
            {
                return BadRequest("Member data is null.");
            }

            /*
             * Scenario 1: Creating a NEW Person and Member at the same time:
             * {
                 "memberID": 0,
                 "personID": 0,
                 "person": {
                   "personID": 0,
                   "firstname": "John",
                   "lastname": "Doe",
                   "email": "john.doe@gym.com",
                   "phone": "5551234567",
                   "username": "johndoe",
                   "password": "SecurePassword123!",
                   "role": 2
                 }
                }               
             
            Scenario 2: Linking an EXISTING Person to be a Member:
                        {
              "memberID": 0,
              "personID": 5, 
              "person": null
            }

            Scenario 3: Testing Validation (This should FAIL):
                            {
                  "memberID": 0,
                  "personID": 0,
                  "person": {
                    "personID": 0,
                    "firstname": "Bad",
                    "lastname": "User",
                    "email": "not-an-email", 
                    "phone": "",
                    "username": "bu",
                    "password": "123",
                    "role": 2
                  }
                }

             */

            try
            {
                if(member.person is not null)
                {
                    _context.Members.Add(member);// You don't need to check if ID exists, because we are creating a NEW one.
                                                 // Entity Framework will automatically:
                                                 // 1. Insert the Person
                                                 // 2. Get the new PersonID
                                                 // 3. Insert the Member with that PersonID
                                                 // All in one step!
                }
                else
                {
                    bool ExistingPerson = await _context.People.AnyAsync(x => x.PersonID == member.PersonID);
                    if(!ExistingPerson)
                    {
                        return BadRequest($"PersonID {member.PersonID} does not exist. And It has not been created with member in the first step, in the if block.");
                    }

                    bool alreadyMember = await _context.Members.Include(x => x.person).AnyAsync(t => t.PersonID == member.PersonID);
                    if(alreadyMember)
                    {
                        return Conflict($"PersonID {member.PersonID} is already registered as a Member.");
                    }
                    _context.Members.Add(member);

                }
                await _context.SaveChangesAsync();
                return CreatedAtAction("GetMember", new { id = member.MemberID }, member);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error creating member: " + ex.Message);
            }
        }

        //https://localhost:7085/api/Members/UpdateMember?id=
        [HttpPut("UpdateMember", Name = "UpdateMember")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutMember(int id, Member member)
        {
            if(id < 0 )
            {
                return BadRequest("This Is An Invalid ID");
            }

            if (id != member.MemberID || member == null)
            {
                return BadRequest("ID mismatch or invalid data for member.");
            }

            var ExistingMember = await _context.Members.Include(m => m.person).FirstOrDefaultAsync(x => x.MemberID == id);

            if(ExistingMember is null)
            {
                return NotFound($"Member with ID {id} not found.");
            }

            if(ExistingMember.person != null && member.person != null)
            {
                ExistingMember.person.Firstname = member.person.Firstname;
                ExistingMember.person.Lastname = member.person.Lastname;
                ExistingMember.person.Phone = member.person.Phone;
            }

            /*
             The Correct JSON Request:
            {
              "memberID": 7,
              "personID": 10,   // <--- MUST MATCH THE REAL DATABASE ID
              "person": {
                "personID": 10, // <--- MUST MATCH THE REAL DATABASE ID
                "firstname": "Updated Name",
                "lastname": "Updated Lastname",
                "email": "newemail@example.com",
                "phone": "1234567890",
                "username": "newuser",
                "password": "newpassword",
                "role": 2
              }
            }
             */


            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating member: " + ex.Message);
            }

            return NoContent();
        }

        // DELETE: api/Members/5
        [HttpDelete("DeleteMember", Name = "DeleteMember")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteMember(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid ID");
            }

            try
            {
                var member = await _context.Members.FindAsync(id);
                if (member == null)
                {
                    return NotFound($"Member with ID {id} not found.");
                }

                _context.Members.Remove(member);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleting member: " + ex.Message);
            }
        }

        [Authorize]
        [HttpPost("RegisterMe", Name = "RegisterMeAsMember")]
        public async Task<ActionResult<Member>> RegisterMe()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("No authenticated user.");

            // 1) Find Person for this user
            var person = await _context.People.FirstOrDefaultAsync(p => p.UserId == userId);
            if (person == null)
                return BadRequest("Person profile not found. Complete registration first.");

            // 2) Prevent duplicate Member
            bool alreadyMember = await _context.Members.AnyAsync(m => m.PersonID == person.PersonID);
            if (alreadyMember)
                return Conflict("You are already registered as a Member.");

            // 3) Create Member linked to Person
            var member = new Member
            {
                PersonID = person.PersonID
            };

            _context.Members.Add(member);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMember), new { id = member.MemberID }, member);
        }

        private bool MemberExists(int id)
        {
            return _context.Members.Any(e => e.MemberID == id);
        }
    }
}