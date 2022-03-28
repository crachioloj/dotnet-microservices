using System.Collections.Generic;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Contracts;
using Play.Catalog.Service.Dtos;
using Play.Catalog.Service.Entities;
using Play.Common;

namespace Play.Catalog.Service.Controllers
{
	[ApiController]
	[Route("items")]
	public class ItemsController : ControllerBase
	{
		private readonly IRepository<Item> itemsRepository;
		private readonly IPublishEndpoint publishEndpoint;

		public ItemsController(IRepository<Item> repository, IPublishEndpoint publishEndpoint)
		{
			this.itemsRepository = repository;
			this.publishEndpoint = publishEndpoint;
		}

		[HttpGet]
		public async Task<ActionResult<IEnumerable<ItemDto>>> GetAsync()
		{
			var items = (await itemsRepository.GetAllAsync())
						.Select(item => item.AsDto());

			return Ok(items);
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id)
		{
			var item = await itemsRepository.GetAsync(id);

			if (item is null)
			{
				return NotFound();
			}

			return item.AsDto();
		}

		[HttpPost]
		public async Task<ActionResult<ItemDto>> PostAsync(CreateItemDto itemDto)
		{
			var item = new Item
			{
				Name = itemDto.Name,
				Description = itemDto.Description,
				Price = itemDto.Price,
				CreatedDate = DateTimeOffset.UtcNow
			};

			await itemsRepository.CreateAsync(item);

			await publishEndpoint.Publish(new CatalogItemCreated(item.Id, item.Name, item.Description));

			return CreatedAtAction(nameof(GetByIdAsync), new { id = item.Id }, item);
		}

		[HttpPut]
		public async Task<IActionResult> PutAsync(Guid id, UpdateItemDto updateItemDto)
		{
			var existingItem = await itemsRepository.GetAsync(id);
			if (existingItem is null)
			{
				return NotFound();
			}

			existingItem.Name = updateItemDto.Name;
			existingItem.Description = updateItemDto.Description;
			existingItem.Price = updateItemDto.Price;

			await itemsRepository.UpdateAsync(existingItem);

			await publishEndpoint.Publish(new CatalogItemUpdated(existingItem.Id, existingItem.Name, existingItem.Description));

			return NoContent();
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteAsync(Guid id)
		{
			var existingItem = await itemsRepository.GetAsync(id);
			if (existingItem is null)
			{
				return NotFound();
			}

			await itemsRepository.RemoveAsync(existingItem.Id);
			
			await publishEndpoint.Publish(new CatalogItemDeleted(existingItem.Id));
			
			return NoContent();
		}
	}
}