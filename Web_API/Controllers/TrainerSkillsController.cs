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
using Web_API.DTOs;


namespace Web_API.Controllers
{
    // [Route("api/[controller]")]
    // [Authorize]
    [Route("api/TrainerSkills")]
    [ApiController]
    public class TrainerSkillsController : ControllerBase
    {
        private readonly ProjectDbContext _context;

        public TrainerSkillsController(ProjectDbContext context)
        {
            _context = context;
        }

        //https://localhost:7085/api/TrainerSkills/GetTrainerSkills
        [HttpGet("GetTrainerSkills",Name = "GetTrainerSkills")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<Object>>> GetTrainerSkills()
        {
            try
            {
                // Including related data so you can see names, not just numbers
                var skills = await _context.TrainerSkills
                                .Include(ts => ts.Trainer).ThenInclude(t => t.person) //If you try to use .Include twice in a row for a deep relationship,
                                                                                      //it won't work because after the first .Include, the computer is "holding" the original object (Trainer),
                                                                                      //not the child object (Skill).
                                .Include(ts => ts.service)
                                .Select(ts => new
                                {
                                    // We create a new "Anonymous Object" with ONLY the data we want
                                    Id = ts.Id,
                                    TrainerName = ts.Trainer.person.Firstname + " " + ts.Trainer.person.Lastname,
                                    ServiceName = ts.service.ServiceName
                                })
                                .ToListAsync();


                if (skills == null || skills.Count == 0)
                {
                    return NotFound("No trainer skills found.");
                }

                return Ok(skills);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving skills: " + ex.Message);
            }
        }

        //https://localhost:7085/api/TrainerSkills/GetTrainerSkill?id=
        [HttpGet("GetTrainerSkill", Name = "GetTrainerSkill")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TrainerSkill>> GetTrainerSkill(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Invalid ID");
                }
                // 1. Use 'Where' to find the ID.
                // 2. Use 'Select' to pick the data you want (avoids the infinite loop crash).
                // 3. Use 'FirstOrDefaultAsync' to execute and get the single object.

                var trainerSkill = await _context.TrainerSkills.Where(ts => ts.Id == id)
                                        .Select(ts => new
                                        {
                                            id = ts.Id,
                                            TrainerName = ts.Trainer.person.Firstname + " " + ts.Trainer.person.Lastname,
                                            ServiceName = ts.service.ServiceName

                                        }).FirstOrDefaultAsync();
                                    
                                        

                if (trainerSkill == null)
                {
                    return NotFound($"TrainerSkill with ID {id} not found.");
                }

                return Ok(trainerSkill);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving skill: " + ex.Message);
            }
        }

        //https://localhost:7085/api/TrainerSkills/PostTrainerSkill
        [HttpPost("PostTrainerSkill", Name = "AddTrainerSkill")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)] // For duplicates
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TrainerSkill>> PostTrainerSkill(TrainerSkill trainerSkill)
        {
            //this method can work with this json :
            //{
            //  "id": 0,
            //  "trainerId": 2,
            //  "serviceId": 2,
            //  "trainer": null,
            //  "service": null
            //}

            // 1. Basic Validation
            if (trainerSkill == null)
            {
                return BadRequest("Skill data is null.");
            }

            // 2. Integrity Checks (Does Trainer exist? Does Service exist?)
            bool trainerExists = await _context.Trainers.AnyAsync(t => t.TrainerID == trainerSkill.TrainerId);
            if (!trainerExists)
            {
                return BadRequest($"TrainerID {trainerSkill.TrainerId} does not exist.");
            }

            bool serviceExists = await _context.Services.AnyAsync(s => s.ServiceID == trainerSkill.ServiceId);
            if (!serviceExists)
            {
                return BadRequest($"ServiceID {trainerSkill.ServiceId} does not exist.");
            }

            // 3. Duplicate Check (Prevent assigning the same skill twice to the same trainer)
            bool alreadyExists = await _context.TrainerSkills.AnyAsync(ts =>
                ts.TrainerId == trainerSkill.TrainerId &&
                ts.ServiceId == trainerSkill.ServiceId);

            if (alreadyExists)
            {
                return Conflict("This trainer already has this skill assigned.");
            }

            try
            {
                _context.TrainerSkills.Add(trainerSkill);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetTrainerSkill", new { id = trainerSkill.Id }, trainerSkill);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error assigning skill: " + ex.Message);
            }
        }

        // https://localhost:7085/api/TrainerSkills/PutTrainerSkill?id=
        [HttpPut("PutTrainerSkill", Name = "UpdateTrainerSkill")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutTrainerSkill(int id, TrainerSkill trainerSkill)
        {
            if (id != trainerSkill.Id || trainerSkill == null)
            {
                return BadRequest("ID mismatch or invalid data.");
            }
            /*the json that it can work with: 
            //  "id": 10,          // <--- MUST MATCH THE URL ID
            //  "trainerId": 2,    // Keeping the same trainer
            //  "serviceId": 5,    // Changing the service to ID 5
            //  "trainer": null,   // Safe to be null
            //  "service": null    // Safe to be null
            //}*/

        _context.Entry(trainerSkill).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TrainerSkillExists(id))
                {
                    return NotFound($"TrainerSkill with ID {id} not found.");
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Concurrency error during update.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating skill: " + ex.Message);
            }

            return NoContent();
        }

        // DELETE: api/TrainerSkills/5
        [HttpDelete("DeleteTrainerSkill", Name = "DeleteTrainerSkill")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteTrainerSkill(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid ID");
            }

            try
            {
                var trainerSkill = await _context.TrainerSkills.FindAsync(id);
                if (trainerSkill == null)
                {
                    return NotFound($"TrainerSkill with ID {id} not found.");
                }

                _context.TrainerSkills.Remove(trainerSkill);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleting skill: " + ex.Message);
            }
        }

        [HttpPost("AddMySkill", Name = "AddMySkill")]
        public async Task<IActionResult> AddMySkill(AddMySkillDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var person = await _context.People.FirstOrDefaultAsync(p => p.UserId == userId);
            if (person == null) return BadRequest("No Person profile found for this user.");

            var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.PersonID == person.PersonID);
            if (trainer == null) return BadRequest("You are not registered as a Trainer.");

            bool serviceExists = await _context.Services.AnyAsync(s => s.ServiceID == dto.ServiceId);
            if (!serviceExists) return BadRequest($"ServiceID {dto.ServiceId} does not exist.");

            bool alreadyExists = await _context.TrainerSkills.AnyAsync(ts =>
                ts.TrainerId == trainer.TrainerID && ts.ServiceId == dto.ServiceId);

            if (alreadyExists) return Conflict("You already have this skill.");

            var trainerSkill = new TrainerSkill
            {
                TrainerId = trainer.TrainerID,
                ServiceId = dto.ServiceId
            };

            _context.TrainerSkills.Add(trainerSkill);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Skill added", trainerSkill.Id });
        }

        [HttpGet("MySkills", Name = "MySkill")]
        public async Task<IActionResult> MySkills()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var person = await _context.People.FirstOrDefaultAsync(p => p.UserId == userId);
            if (person == null) return BadRequest("No Person profile found for this user.");

            var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.PersonID == person.PersonID);
            if (trainer == null) return BadRequest("You are not registered as a Trainer.");

            var skills = await _context.TrainerSkills
                .Where(ts => ts.TrainerId == trainer.TrainerID)
                .Include(ts => ts.service)
                .Select(ts => new { ts.Id, ts.ServiceId, ServiceName = ts.service.ServiceName })
                .ToListAsync();

            return Ok(skills);
        }

        private bool TrainerSkillExists(int id)
        {
            return _context.TrainerSkills.Any(e => e.Id == id);
        }
    }
}