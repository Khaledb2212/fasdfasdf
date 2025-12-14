using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_API.Models;
using System.Security.Claims;
using Web_API.DTOs;

namespace Web_API.Controllers
{
    //[Authorize]
    //[Route("api/[controller]")]
    [Route("api/TrainerAvailabilities")]
    [ApiController]
    public class TrainerAvailabilitiesController : ControllerBase
    {
        private readonly ProjectDbContext _context;

        public TrainerAvailabilitiesController(ProjectDbContext context)
        {
            _context = context;
        }

        //https://localhost:7085/api/TrainerAvailabilities/GetTrainerAvailabilities
        [HttpGet("GetTrainerAvailabilities",Name = "GetTrainerAvailabilities")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<TrainerAvailability>>> GetTrainerAvailabilities()
        {
            try
            {
                // Include Trainer and Service info so the schedule is readable
                var availabilities = await _context.TrainerAvailabilities
                                                   .Include(ta => ta.Trainer)
                                                        .ThenInclude(t => t.person) // See Trainer Name
                                                   .Include(ta => ta.Service)       // See Service Name
                                                   .ToListAsync();

                if (availabilities == null || availabilities.Count == 0)
                {
                    return NotFound("No schedules found.");
                }

                return Ok(availabilities);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving schedules: " + ex.Message);
            }
        }

        //https://localhost:7085/api/TrainerAvailabilities/GetTrainerAvailability?id=
        [HttpGet("GetTrainerAvailability", Name = "GetTrainerAvailability")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TrainerAvailability>> GetTrainerAvailability(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Invalid ID");
                }

                var trainerAvailability = await _context.TrainerAvailabilities
                                                        .Include(ta => ta.Trainer)
                                                        .Include(ta => ta.Service)
                                                        .FirstOrDefaultAsync(ta => ta.AvailabilityId == id);

                if (trainerAvailability == null)
                {
                    return NotFound($"Schedule with ID {id} not found.");
                }

                return Ok(trainerAvailability);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving schedule: " + ex.Message);
            }
        }

        //https://localhost:7085/api/TrainerAvailabilities/PostTrainerAvailability
        [HttpPost("PostTrainerAvailability",Name = "AddTrainerAvailability")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TrainerAvailability>> PostTrainerAvailability(TrainerAvailability trainerAvailability)
        {
            // 1. Basic Validation
            if (trainerAvailability == null)
            {
                return BadRequest("Schedule data is null.");
            }

            /*the json format to work with this is : 
             {
              "availabilityId": 0,
              "dayOfWeek": 5,
              "startTime": "2024-01-01T09:00:00",
              "endTime": "2024-01-01T17:00:00",
              "trainerId": 2,
              "serviceTypeId": 5,
              "trainer": null,
              "service": null
            }
            */

            // 2. Time Logic Check (Start must be before End)
            if (trainerAvailability.StartTime >= trainerAvailability.EndTime)
            {
                return BadRequest("Start Time must be earlier than End Time.");
            }

            // 3. Integrity Checks
            bool trainerExists = await _context.Trainers.AnyAsync(t => t.TrainerID == trainerAvailability.TrainerId);
            if (!trainerExists)
            {
                return BadRequest($"TrainerID {trainerAvailability.TrainerId} does not exist.");
            }

            bool serviceExists = await _context.Services.AnyAsync(s => s.ServiceID == trainerAvailability.ServiceTypeId);
            if (!serviceExists)
            {
                return BadRequest($"ServiceID {trainerAvailability.ServiceTypeId} does not exist.");
            }

            bool hasSkill = await _context.TrainerSkills.AnyAsync(ts =>
                ts.TrainerId == trainerAvailability.TrainerId &&
                ts.ServiceId == trainerAvailability.ServiceTypeId);

            if (!hasSkill)
            {
                return BadRequest($"Trainer {trainerAvailability.TrainerId} is not qualified for Service {trainerAvailability.ServiceTypeId}. Add this skill in the TrainerSkills table first.");
            }   

            try
            {
                _context.TrainerAvailabilities.Add(trainerAvailability);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetTrainerAvailability", new { id = trainerAvailability.AvailabilityId }, trainerAvailability);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error creating schedule: " + ex.Message);
            }
        }

        //https://localhost:7085/api/TrainerAvailabilities/PutTrainerAvailability?id=
        [HttpPut("PutTrainerAvailability", Name = "UpdateTrainerAvailability")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
         public async Task<IActionResult> PutTrainerAvailability(int id, TrainerAvailability trainerAvailability)
          {
            if (id != trainerAvailability.AvailabilityId || trainerAvailability == null)
            {
                return BadRequest("ID mismatch or invalid data.");
            }

            /*the json format to work with this is : 
             {
              "availabilityId": 0, <-- Do Not Forget to match it in the URL
              "dayOfWeek": 5,
              "startTime": "2024-01-01T09:00:00",
              "endTime": "2024-01-01T17:00:00",
              "trainerId": 2,
              "serviceTypeId": 5,
              "trainer": null,
              "service": null
            }
            */

            

            if (trainerAvailability.StartTime >= trainerAvailability.EndTime)
            {
                return BadRequest("Start Time must be earlier than End Time.");
            }

            bool hasSkill = await _context.TrainerSkills.AnyAsync(ts =>
             ts.TrainerId == trainerAvailability.TrainerId &&
             ts.ServiceId == trainerAvailability.ServiceTypeId);

            if (!hasSkill)
            {
                return BadRequest($"Trainer {trainerAvailability.TrainerId} does not have the skill for Service {trainerAvailability.ServiceTypeId}. Please assign the skill in the TrainerSkills table first.");
            }

            _context.Entry(trainerAvailability).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TrainerAvailabilityExists(id))
                {
                    return NotFound($"Schedule with ID {id} not found.");
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Concurrency error during update.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating schedule: " + ex.Message);
            }

            return NoContent();
        }
    
        //https://localhost:7085/api/TrainerAvailabilities/DeleteTrainerAvailability?id=
        [HttpDelete("DeleteTrainerAvailability", Name = "DeleteTrainerAvailability")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteTrainerAvailability(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid ID");
            }

            try
            {
                var trainerAvailability = await _context.TrainerAvailabilities.FindAsync(id);
                if (trainerAvailability == null)
                {
                    return NotFound($"Schedule with ID {id} not found.");
                }

                _context.TrainerAvailabilities.Remove(trainerAvailability);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleting schedule: " + ex.Message);
            }
        }


        [HttpPost("AddMySlot", Name = "AddMySlot")]
        public async Task<IActionResult> AddMySlot(AddAvailabilityDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var anchor = new DateTime(2000, 1, 1);

            var start = anchor.Add(dto.StartTime.TimeOfDay);
            var end = anchor.Add(dto.EndTime.TimeOfDay);

            //if (!TimeSpan.TryParse(dto.StartTime, out var start))
            //    return BadRequest("StartTime must be like '09:00'.");

            //if (!TimeSpan.TryParse(dto.EndTime, out var end))
            //    return BadRequest("EndTime must be like '12:00'.");

            if (start >= end)
                return BadRequest("StartTime must be < EndTime.");

            // 1) Identify the logged-in user (shared cookie)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("No authenticated user (cookie not received/decrypted).");

            // 2) Find Person by UserId
            var person = await _context.People.FirstOrDefaultAsync(p => p.UserId == userId);
            if (person == null)
                return BadRequest("No Person profile found for this user.");

            // 3) Find Trainer record for that Person
            var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.PersonID == person.PersonID);
            if (trainer == null)
                return BadRequest("You are not registered as a Trainer.");

            // 4) Validate ServiceTypeId exists
            bool serviceExists = await _context.Services.AnyAsync(s => s.ServiceID == dto.ServiceTypeId);
            if (!serviceExists)
                return BadRequest($"ServiceTypeId {dto.ServiceTypeId} does not exist.");

            // IMPORTANT:
            // Your model stores StartTime/EndTime as DateTime but you want "time of day".
            // So we store everything using a fixed date to make comparisons consistent.
            var baseDate = new DateTime(2000, 1, 1);
            var startDt = baseDate.Add(start.TimeOfDay);
            var endDt = baseDate.Add(end.TimeOfDay);

            // 5) Prevent overlap for the same trainer/day
            // (Even if ServiceTypeId is different, a trainer can't be available for two sessions at the same time)
            bool overlaps = await _context.TrainerAvailabilities.AnyAsync(a =>
                a.TrainerId == trainer.TrainerID &&
                a.DayOfWeek == dto.DayOfWeek &&
                startDt < a.EndTime &&
                endDt > a.StartTime);

            if (overlaps)
                return Conflict("This slot overlaps an existing slot.");

            var slot = new TrainerAvailability
            {
                TrainerId = trainer.TrainerID,
                DayOfWeek = dto.DayOfWeek,
                StartTime = startDt,
                EndTime = endDt,
                ServiceTypeId = dto.ServiceTypeId
            };

            _context.TrainerAvailabilities.Add(slot);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Slot added",
                slot.AvailabilityId
            });
        }

        // GET: api/TrainerAvailabilities/MySlots
        [HttpGet("MySlots", Name = "MySlots")]
        public async Task<IActionResult> MySlots()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var person = await _context.People.FirstOrDefaultAsync(p => p.UserId == userId);
            if (person == null) return BadRequest("No Person profile found for this user.");

            var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.PersonID == person.PersonID);
            if (trainer == null) return BadRequest("You are not registered as a Trainer.");

            var slots = await _context.TrainerAvailabilities
                .Where(a => a.TrainerId == trainer.TrainerID)
                .Include(a => a.Service)
                .OrderBy(a => a.DayOfWeek)
                .ThenBy(a => a.StartTime)
                .Select(a => new
                {
                    a.AvailabilityId,
                    a.DayOfWeek,
                    StartTime = a.StartTime.ToString("HH:mm"),
                    EndTime = a.EndTime.ToString("HH:mm"),
                    a.ServiceTypeId,
                    ServiceName = a.Service != null ? a.Service.ServiceName : null
                })
                .ToListAsync();

            return Ok(slots);
        }
        
        
        private bool TrainerAvailabilityExists(int id)
        {
            return _context.TrainerAvailabilities.Any(e => e.AvailabilityId == id);
        }
    }
}