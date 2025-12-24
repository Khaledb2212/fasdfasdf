//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Security.Claims;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Web_API.DTOs;
//using Web_API.Models;

//namespace Web_API.Controllers
//{
//    //[Route("api/[controller]")]
//    [Route("api/Trainers")]
//    [ApiController]
//    public class TrainersController : ControllerBase
//    {
//        private readonly Web_API.Models.ProjectDbContext _context;

//        public TrainersController(ProjectDbContext context)
//        {
//            _context = context;
//        }

//        //https://localhost:7085/api/Trainers/GetTrainers
//        [HttpGet("GetTrainers", Name = "GetTrainers")]
//        [ProducesResponseType(StatusCodes.Status200OK)]
//        [ProducesResponseType(StatusCodes.Status404NotFound)]
//        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//        public async Task<ActionResult<IEnumerable<Trainer>>> GetTrainers()
//        {
//            try
//            {
//                // Including the 'Person' data so you see the trainer's name, not just ID
//                var trainers = await _context.Trainers.Include(x => x.person).ToListAsync();

//                if (trainers == null || trainers.Count == 0)
//                {
//                    return NotFound("No trainers found.");
//                }

//                return Ok(trainers);
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving data: " + ex.Message);
//            }
//        }

//        //https://localhost:7085/api/Trainers/GetTrainer?id=
//        [HttpGet("GetTrainer", Name = "GetTrainer")]
//        [ProducesResponseType(StatusCodes.Status200OK)]
//        [ProducesResponseType(StatusCodes.Status404NotFound)]
//        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//        public async Task<ActionResult<Trainer>> GetTrainer(int id)
//        {
//            try
//            {
//                if (id <= 0)
//                {
//                    return BadRequest("Invalid ID");
//                }

//                var trainer = await _context.Trainers

//                        .Include(t => t.person)
//                        .Include(t => t.Skills)
//                            // 3. IMPORTANT: Load the Service details (Name) inside the skill
//                            .ThenInclude(ts => ts.service)

//                        .FirstOrDefaultAsync(t => t.TrainerID == id);

//                if (trainer == null)
//                {
//                    return NotFound($"Trainer with ID {id} not found.");
//                }

//                return Ok(trainer);
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving trainer: " + ex.Message);
//            }
//        }

//        //https://localhost:7085/api/Trainers/AddTrainer
//        [HttpPost("AddTrainer",Name = "AddTrainer")]
//        [ProducesResponseType(StatusCodes.Status201Created)]
//        [ProducesResponseType(StatusCodes.Status400BadRequest)]
//        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//        [ProducesResponseType(StatusCodes.Status409Conflict)]
//        public async Task<ActionResult<Trainer>> PostTrainer(Trainer trainer)
//        {
//            // 1. Basic Validation
//            if (trainer == null)
//            {
//                return BadRequest("Trainer data is null.");
//            }

//            // 2. Business Logic Validation: Ensure the Person exists first!
//            // You cannot make a ghost a trainer.
//            try
//            {
//                if (trainer.person is not null)// i do not mean that it is null in the database, but it isn`t null in the json file
//                {

//                }
//                else
//                {
//                    bool ExistingPerson = await _context.People.AnyAsync(x => x.PersonID == trainer.PersonID);
//                    if (!ExistingPerson)
//                    {
//                        return BadRequest($"PersonID {trainer.PersonID} does not exist. And It has not been created with trainer in the first step, in the if block.");

//                    }
//                    bool alreadyTrainer = await _context.Trainers.AnyAsync(m => m.PersonID == trainer.PersonID);
//                    if (alreadyTrainer)
//                    {
//                        return Conflict($"PersonID {trainer.PersonID} is already registered as a trainer.");
//                    }                    
//                }
//                await _context.Trainers.AddAsync(trainer);
//                await _context.SaveChangesAsync();
//                return CreatedAtAction("GetTrainer", new { id = trainer.TrainerID }, trainer);
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(StatusCodes.Status500InternalServerError, "Error creating trainer: " + ex.Message);
//            }

//        }

//        //https://localhost:7085/api/Trainers/UpdateTrainer?id=
//        [HttpPut("UpdateTrainer", Name = "UpdateTrainer")]
//        [ProducesResponseType(StatusCodes.Status204NoContent)]
//        [ProducesResponseType(StatusCodes.Status400BadRequest)]
//        [ProducesResponseType(StatusCodes.Status404NotFound)]
//        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//        public async Task<IActionResult> PutTrainer(int id, Trainer trainer)
//        {
//            // 1. ID Validation
//            if (id <= 0)
//            {
//                return BadRequest("This Is An Invalid ID");
//            }

//            if (id != trainer.TrainerID || trainer == null)
//            {
//                return BadRequest("ID mismatch or invalid data for trainer.");
//            }

//            // 2. Load Existing Trainer AND the related Person
//            // We search by TrainerID because 'id' comes from the URL (api/Trainers/5)
//            var existingTrainer = await _context.Trainers
//                                                .Include(t => t.person)
//                                                .FirstOrDefaultAsync(t => t.TrainerID == id);

//            if (existingTrainer == null)
//            {
//                return NotFound($"Trainer with ID {id} not found.");
//            }

//            // 3. Update Person Properties (The Common Data)
//            if (existingTrainer.person != null && trainer.person != null)
//            {
//                existingTrainer.person.Firstname = trainer.person.Firstname;
//                existingTrainer.person.Lastname = trainer.person.Lastname;
//                existingTrainer.person.Phone = trainer.person.Phone;
//                // Role is usually not updated here to prevent security issues
//            }

//            // 4. Update Trainer Specific Properties (The New Part)
//            existingTrainer.ExpertiseAreas = trainer.ExpertiseAreas;
//            existingTrainer.Description = trainer.Description;

//            /*
//             The Correct JSON Request for Trainer:
//             {
//               "trainerID": 3,
//               "personID": 10,   // Must match the real PersonID in DB
//               "expertiseAreas": "Advanced Yoga, Pilates",
//               "description": "Updated description for the trainer.",
//               "person": {
//                 "personID": 10, // Must match the real PersonID in DB
//                 "firstname": "Ali",
//                 "lastname": "Demir",
//                 "email": "ali.updated@gym.com",
//                 "phone": "5559998877",
//                 "username": "ali_tr",
//                 "password": "newpassword123",
//                 "role": 1
//               }
//             }
//             */

//            try
//            {
//                await _context.SaveChangesAsync();
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating trainer: " + ex.Message);
//            }

//            return NoContent();
//        }


//        // DELETE: api/Trainers/5
//        [HttpDelete("DeleteTrainer", Name = "DeleteTrainer")]
//        [ProducesResponseType(StatusCodes.Status204NoContent)]
//        [ProducesResponseType(StatusCodes.Status404NotFound)]
//        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//        public async Task<IActionResult> DeleteTrainer(int id)
//        {
//            if (id <= 0)
//            {
//                return BadRequest("Invalid ID");
//            }

//            try
//            {
//                var trainer = await _context.Trainers.FindAsync(id);
//                if (trainer == null)
//                {
//                    return NotFound($"Trainer with ID {id} not found.");
//                }

//                _context.Trainers.Remove(trainer);
//                await _context.SaveChangesAsync();

//                return NoContent();
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleting trainer: " + ex.Message);
//            }
//        }

//        [Authorize]
//        [HttpPost("RegisterMe", Name = "RegisterMeAsTrainer")]
//        [ProducesResponseType(StatusCodes.Status204NoContent)]
//        [ProducesResponseType(StatusCodes.Status404NotFound)]
//        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
//        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//        public async Task<ActionResult<Trainer>> RegisterMe(CreateTrainerDto dto)
//        {
//            if (!ModelState.IsValid) return BadRequest(ModelState);

//            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
//            if (string.IsNullOrWhiteSpace(userId))
//                return Unauthorized("No authenticated user.");

//            // 1) Find Person for this user
//            var person = await _context.People.FirstOrDefaultAsync(p => p.UserId == userId);
//            if (person == null)
//                return BadRequest("Person profile not found. Complete registration first.");

//            // 2) Prevent duplicate Trainer
//            bool alreadyTrainer = await _context.Trainers.AnyAsync(t => t.PersonID == person.PersonID);
//            if (alreadyTrainer)
//                return Conflict("You are already registered as a Trainer.");

//            // 3) Create Trainer linked to Person
//            var trainer = new Trainer
//            {
//                PersonID = person.PersonID,
//                ExpertiseAreas = dto.ExpertiseAreas,
//                Description = dto.Description
//            };

//            _context.Trainers.Add(trainer);
//            await _context.SaveChangesAsync();

//            return CreatedAtAction(nameof(GetTrainer), new { id = trainer.TrainerID }, trainer);
//        }

//        [HttpGet("Available", Name = "Available")]
//        [ProducesResponseType(StatusCodes.Status204NoContent)]
//        [ProducesResponseType(StatusCodes.Status404NotFound)]
//        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
//        public async Task<ActionResult<Trainer>> Available(DateTime date, int serviceId)
//        {
//            if(serviceId < 0)
//            {
//                return BadRequest("The service Id is Invaild");
//            }

//            if(date < DateTime.Now.Date)
//            {
//                return BadRequest("The Date is invalid");
//            }

//            int day = (int)date.DayOfWeek;

//            var list = await _context.TrainerAvailabilities
//              .Where(a => a.DayOfWeek == day && a.ServiceTypeId == serviceId)
//              .Join(_context.Trainers.Include(t => t.person),
//                  a => a.TrainerId,
//                  t => t.TrainerID,
//                  (a, t) => new
//                  {
//                      t.TrainerID,
//                      TrainerName = (t.person != null ? (t.person.Firstname + " " + t.person.Lastname) : "Unknown"),
//                      a.DayOfWeek,
//                      StartTime = a.StartTime.ToString("HH:mm"),
//                      EndTime = a.EndTime.ToString("HH:mm"),
//                      ServiceId = a.ServiceTypeId
//                  })
//              .Distinct()
//              .OrderBy(x => x.TrainerName)
//              .ToListAsync();

//            if (list.Count == 0) return NotFound("No available trainers for that date/service.");

//            return Ok(list);

//        }


//        [HttpPost("AvailableWithDTO", Name = "AvailableWithDTO")]
//        public async Task<IActionResult> AvailableWithDTO(AvailableTrainersRequestDto dto)
//        {
//            if (!ModelState.IsValid) return BadRequest(ModelState);

//            if (dto.ServiceId <= 0)
//                return BadRequest("Invalid serviceId.");

//            if (dto.Date.Date < DateTime.Today)
//                return BadRequest("Date cannot be in the past.");

//            if (!TimeSpan.TryParse(dto.Start, out var startTs))
//                return BadRequest("Start must be like '09:00'.");

//            if (!TimeSpan.TryParse(dto.End, out var endTs))
//                return BadRequest("End must be like '10:00'.");

//            if (startTs >= endTs)
//                return BadRequest("Start must be < End.");

//            int day = (int)dto.Date.DayOfWeek;

//            //// Availability is stored as DateTime anchored (e.g., 2000-01-01 + time)
//            //var anchor = new DateTime(2026, 1, 1);
//            //var reqStart = anchor.Add(startTs);
//            //var reqEnd = anchor.Add(endTs);

//            // 1. Get ALL slots for this Day and Service first
//            // We do NOT filter by time in SQL anymore to avoid Year mismatches.
//            var potentialSlots = await _context.TrainerAvailabilities
//                .Where(a => a.DayOfWeek == day && a.ServiceTypeId == dto.ServiceId)
//                .ToListAsync();

//            // 2. Filter in Memory using TimeOfDay (Ignores Year/Month/Date)
//            // This works whether your DB has year 2000, 2025, or 2026.
//            var slotTrainerIds = potentialSlots
//                .Where(a => startTs >= a.StartTime.TimeOfDay && endTs <= a.EndTime.TimeOfDay)
//                .Select(a => a.TrainerId)
//                .Distinct()
//                .ToList();

//            // Real datetime for appointments
//            var reqStartAt = dto.Date.Date.Add(startTs);
//            var reqEndAt = dto.Date.Date.Add(endTs);

//            //// Trainers who have a slot containing this requested time
//            //var slotTrainerIds = _context.TrainerAvailabilities
//            //    .Where(a =>
//            //        a.DayOfWeek == day &&
//            //        a.ServiceTypeId == dto.ServiceId &&
//            //        reqStart >= a.StartTime &&
//            //        reqEnd <= a.EndTime
//            //    )
//            //    .Select(a => a.TrainerId)
//            //    .Distinct();

//            // Trainers busy due to existing appointments
//            var busyTrainerIds = _context.Appointments
//                .Where(a => reqStartAt < a.EndTime && reqEndAt > a.StartTime)
//                .Select(a => a.TrainerID)
//                .Distinct();

//            var available = await _context.Trainers
//                .Include(t => t.person)
//                .Where(t => slotTrainerIds.Contains(t.TrainerID))
//                .Where(t => !busyTrainerIds.Contains(t.TrainerID))
//                .Select(t => new
//                {
//                    t.TrainerID,
//                    TrainerName = t.person != null ? (t.person.Firstname + " " + t.person.Lastname) : "Unknown",
//                    t.ExpertiseAreas,
//                    t.Description
//                })
//                .OrderBy(x => x.TrainerName)
//                .ToListAsync();

//            if (available.Count == 0)
//                return NotFound("No available trainers for that date/time/service.");

//            return Ok(available);
//        }

//        [Authorize(Roles = "Trainer")]
//        [HttpGet("MyProfile", Name = "MyProfile")]
//        public async Task<IActionResult> MyProfile()
//        {
//            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
//            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

//            // Find trainer by the logged-in Identity UserId
//            var trainer = await _context.Trainers
//                .Include(t => t.person)
//                .FirstOrDefaultAsync(t => t.person != null && t.person.UserId == userId);

//            if (trainer == null)
//                return BadRequest("You are not registered as a Trainer.");

//            return Ok(new
//            {
//                trainer.TrainerID,
//                trainer.ExpertiseAreas,
//                trainer.Description
//            });
//        }

//        [Authorize(Roles = "Trainer")]
//        [HttpPut("UpdateMyProfile", Name = "UpdateMyProfile")]
//        public async Task<IActionResult> UpdateMyProfile(CreateTrainerDto dto)
//        {
//            if (!ModelState.IsValid) return BadRequest(ModelState);

//            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
//            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

//            var trainer = await _context.Trainers
//                .Include(t => t.person)
//                .FirstOrDefaultAsync(t => t.person != null && t.person.UserId == userId);

//            if (trainer == null)
//                return BadRequest("You are not registered as a Trainer.");

//            // Only these two fields
//            trainer.ExpertiseAreas = dto.ExpertiseAreas;
//            trainer.Description = dto.Description;

//            await _context.SaveChangesAsync();

//            return NoContent(); // 204
//        }

//        private bool TrainerExists(int id)
//        {
//            return _context.Trainers.Any(e => e.TrainerID == id);
//        }
//    }
//}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_API.DTOs;
using Web_API.Models;

namespace Web_API.Controllers
{
    [Route("api/Trainers")]
    [ApiController]
    public class TrainersController : ControllerBase
    {
        private readonly Web_API.Models.ProjectDbContext _context;

        public TrainersController(ProjectDbContext context)
        {
            _context = context;
        }

        // https://localhost:7085/api/Trainers/GetTrainers
        [HttpGet("GetTrainers", Name = "GetTrainers")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<Trainer>>> GetTrainers()
        {
            try
            {
                // FIX: Added .Include(x => x.Skills).ThenInclude(ts => ts.service)
                // This ensures the list contains the skills needed for the frontend filter.
                var trainers = await _context.Trainers
                    .Include(x => x.person)
                    .Include(x => x.Skills)
                        .ThenInclude(ts => ts.service)
                    .ToListAsync();

                if (trainers == null || trainers.Count == 0)
                {
                    return NotFound("No trainers found.");
                }

                return Ok(trainers);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving data: " + ex.Message);
            }
        }

        // https://localhost:7085/api/Trainers/GetTrainer?id=
        [HttpGet("GetTrainer", Name = "GetTrainer")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Trainer>> GetTrainer(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Invalid ID");
                }

                var trainer = await _context.Trainers
                        .Include(t => t.person)
                        .Include(t => t.Skills)
                            .ThenInclude(ts => ts.service) // Loads Service Name
                        .FirstOrDefaultAsync(t => t.TrainerID == id);

                if (trainer == null)
                {
                    return NotFound($"Trainer with ID {id} not found.");
                }

                return Ok(trainer);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving trainer: " + ex.Message);
            }
        }

        // https://localhost:7085/api/Trainers/AddTrainer
        [HttpPost("AddTrainer", Name = "AddTrainer")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Trainer>> PostTrainer(Trainer trainer)
        {
            if (trainer == null)
            {
                return BadRequest("Trainer data is null.");
            }

            try
            {
                if (trainer.person is not null)
                {
                    // Person object passed in JSON (Create both)
                }
                else
                {
                    // Person ID passed (Link existing)
                    bool ExistingPerson = await _context.People.AnyAsync(x => x.PersonID == trainer.PersonID);
                    if (!ExistingPerson)
                    {
                        return BadRequest($"PersonID {trainer.PersonID} does not exist.");
                    }
                    bool alreadyTrainer = await _context.Trainers.AnyAsync(m => m.PersonID == trainer.PersonID);
                    if (alreadyTrainer)
                    {
                        return Conflict($"PersonID {trainer.PersonID} is already registered as a trainer.");
                    }
                }
                await _context.Trainers.AddAsync(trainer);
                await _context.SaveChangesAsync();
                return CreatedAtAction("GetTrainer", new { id = trainer.TrainerID }, trainer);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error creating trainer: " + ex.Message);
            }
        }

        // https://localhost:7085/api/Trainers/UpdateTrainer?id=
        [HttpPut("UpdateTrainer", Name = "UpdateTrainer")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutTrainer(int id, Trainer trainer)
        {
            if (id <= 0) return BadRequest("This Is An Invalid ID");
            if (id != trainer.TrainerID || trainer == null) return BadRequest("ID mismatch or invalid data.");

            var existingTrainer = await _context.Trainers
                                            .Include(t => t.person)
                                            .FirstOrDefaultAsync(t => t.TrainerID == id);

            if (existingTrainer == null) return NotFound($"Trainer with ID {id} not found.");

            // Update Person Properties
            if (existingTrainer.person != null && trainer.person != null)
            {
                existingTrainer.person.Firstname = trainer.person.Firstname;
                existingTrainer.person.Lastname = trainer.person.Lastname;
                existingTrainer.person.Phone = trainer.person.Phone;
            }

            // Update Trainer Properties
            existingTrainer.ExpertiseAreas = trainer.ExpertiseAreas;
            existingTrainer.Description = trainer.Description;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating trainer: " + ex.Message);
            }

            return NoContent();
        }

        // DELETE: api/Trainers/5
        [HttpDelete("DeleteTrainer", Name = "DeleteTrainer")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteTrainer(int id)
        {
            if (id <= 0) return BadRequest("Invalid ID");

            try
            {
                var trainer = await _context.Trainers.FindAsync(id);
                if (trainer == null) return NotFound($"Trainer with ID {id} not found.");

                _context.Trainers.Remove(trainer);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleting trainer: " + ex.Message);
            }
        }

        [Authorize]
        [HttpPost("RegisterMe", Name = "RegisterMeAsTrainer")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Trainer>> RegisterMe(CreateTrainerDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("No authenticated user.");

            var person = await _context.People.FirstOrDefaultAsync(p => p.UserId == userId);
            if (person == null)
                return BadRequest("Person profile not found. Complete registration first.");

            bool alreadyTrainer = await _context.Trainers.AnyAsync(t => t.PersonID == person.PersonID);
            if (alreadyTrainer)
                return Conflict("You are already registered as a Trainer.");

            var trainer = new Trainer
            {
                PersonID = person.PersonID,
                ExpertiseAreas = dto.ExpertiseAreas,
                Description = dto.Description
            };

            _context.Trainers.Add(trainer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTrainer), new { id = trainer.TrainerID }, trainer);
        }

        [HttpGet("Available", Name = "Available")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Trainer>> Available(DateTime date, int serviceId)
        {
            if (serviceId < 0) return BadRequest("The service Id is Invaild");
            if (date < DateTime.Now.Date) return BadRequest("The Date is invalid");

            int day = (int)date.DayOfWeek;

            var list = await _context.TrainerAvailabilities
              .Where(a => a.DayOfWeek == day && a.ServiceTypeId == serviceId)
              .Join(_context.Trainers.Include(t => t.person),
                  a => a.TrainerId,
                  t => t.TrainerID,
                  (a, t) => new
                  {
                      t.TrainerID,
                      TrainerName = (t.person != null ? (t.person.Firstname + " " + t.person.Lastname) : "Unknown"),
                      a.DayOfWeek,
                      StartTime = a.StartTime.ToString("HH:mm"),
                      EndTime = a.EndTime.ToString("HH:mm"),
                      ServiceId = a.ServiceTypeId
                  })
              .Distinct()
              .OrderBy(x => x.TrainerName)
              .ToListAsync();

            if (list.Count == 0) return NotFound("No available trainers for that date/service.");

            return Ok(list);
        }

        [HttpPost("AvailableWithDTO", Name = "AvailableWithDTO")]
        public async Task<IActionResult> AvailableWithDTO(AvailableTrainersRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (dto.ServiceId <= 0) return BadRequest("Invalid serviceId.");
            if (dto.Date.Date < DateTime.Today) return BadRequest("Date cannot be in the past.");
            if (!TimeSpan.TryParse(dto.Start, out var startTs)) return BadRequest("Start must be like '09:00'.");
            if (!TimeSpan.TryParse(dto.End, out var endTs)) return BadRequest("End must be like '10:00'.");
            if (startTs >= endTs) return BadRequest("Start must be < End.");

            int day = (int)dto.Date.DayOfWeek;

            // 1. Get ALL slots for this Day and Service
            var potentialSlots = await _context.TrainerAvailabilities
                .Where(a => a.DayOfWeek == day && a.ServiceTypeId == dto.ServiceId)
                .ToListAsync();

            // 2. Filter in Memory using TimeOfDay
            var slotTrainerIds = potentialSlots
                .Where(a => startTs >= a.StartTime.TimeOfDay && endTs <= a.EndTime.TimeOfDay)
                .Select(a => a.TrainerId)
                .Distinct()
                .ToList();

            // 3. Check for Conflicts
            var reqStartAt = dto.Date.Date.Add(startTs);
            var reqEndAt = dto.Date.Date.Add(endTs);

            var busyTrainerIds = _context.Appointments
                .Where(a => reqStartAt < a.EndTime && reqEndAt > a.StartTime)
                .Select(a => a.TrainerID)
                .Distinct();

            var available = await _context.Trainers
                .Include(t => t.person)
                .Where(t => slotTrainerIds.Contains(t.TrainerID))
                .Where(t => !busyTrainerIds.Contains(t.TrainerID))
                .Select(t => new
                {
                    t.TrainerID,
                    TrainerName = t.person != null ? (t.person.Firstname + " " + t.person.Lastname) : "Unknown",
                    t.ExpertiseAreas,
                    t.Description
                })
                .OrderBy(x => x.TrainerName)
                .ToListAsync();

            if (available.Count == 0)
                return NotFound("No available trainers for that date/time/service.");

            return Ok(available);
        }



        [Authorize(Roles = "Trainer")]
        [HttpPut("UpdateMyProfile", Name = "UpdateMyProfile")]
        public async Task<IActionResult> UpdateMyProfile(CreateTrainerDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var trainer = await _context.Trainers
                .Include(t => t.person)
                .FirstOrDefaultAsync(t => t.person != null && t.person.UserId == userId);

            if (trainer == null) return BadRequest("You are not registered as a Trainer.");

            trainer.ExpertiseAreas = dto.ExpertiseAreas;
            trainer.Description = dto.Description;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [Authorize(Roles = "Trainer")]
        [HttpGet("MyProfile")]
        public async Task<IActionResult> MyProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var person = await _context.People.FirstOrDefaultAsync(p => p.UserId == userId);
            if (person == null) return BadRequest("No Person profile found for this user.");

            var trainer = await _context.Trainers
                .Include(t => t.person)
                .Include(t => t.Skills)
                    .ThenInclude(ts => ts.service)
                .FirstOrDefaultAsync(t => t.PersonID == person.PersonID);

            if (trainer == null) return BadRequest("You are not registered as a Trainer.");

            return Ok(new
            {
                trainerID = trainer.TrainerID,
                personID = trainer.PersonID,
                person = new
                {
                    firstName = trainer.person?.Firstname,
                    lastName = trainer.person?.Lastname,
                    phone = trainer.person?.Phone,
                    email = User.Identity?.Name
                },
                expertiseAreas = trainer.ExpertiseAreas,
                description = trainer.Description,
                skills = (trainer.Skills ?? new List<TrainerSkill>()).Select(s => new
                {
                    id = s.Id,
                    serviceId = s.ServiceId,
                    service = s.service == null ? null : new
                    {
                        serviceID = s.service.ServiceID,
                        serviceName = s.service.ServiceName,
                        feesPerHour = s.service.FeesPerHour,
                        details = s.service.Details
                    }
                })
            });
        }

        private bool TrainerExists(int id)
        {
            return _context.Trainers.Any(e => e.TrainerID == id);
        }
    }
}