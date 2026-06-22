import { z } from 'zod'

const addressSchema = z.object({
  street: z.string().max(200).optional(),
  city: z.string().min(1, 'City is required').max(100),
  region: z.string().max(100).optional(),
  country: z.string().min(1, 'Country is required').max(100),
})

const coordinatesSchema = z
  .object({
    latitude: z.number().min(-90).max(90),
    longitude: z.number().min(-180).max(180),
  })
  .nullable()

export const locationSchema = z.object({
  address: addressSchema,
  coordinates: coordinatesSchema,
})

export const customerSchema = z.object({
  accountNumber: z.string().min(1, 'Account number is required').max(20),
  personalInfo: z.object({
    firstName: z.string().min(1, 'First name is required').max(100),
    lastName: z.string().min(1, 'Last name is required').max(100),
    gender: z.enum(['Male', 'Female', 'Other', 'PreferNotToSay']),
    phone: z.string().min(1, 'Phone is required').max(20),
    email: z.string().max(200).optional(),
  }),
  location: locationSchema,
})

export type CustomerFormValues = z.input<typeof customerSchema>
export type CustomerValues = z.output<typeof customerSchema>

export const serviceLocationSchema = z.object({
  label: z.string().min(1, 'Label is required').max(200),
  location: locationSchema,
})

export type ServiceLocationFormValues = z.input<typeof serviceLocationSchema>
export type ServiceLocationValues = z.output<typeof serviceLocationSchema>
