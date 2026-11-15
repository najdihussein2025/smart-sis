import { Link, type LinkProps } from 'react-router'
import { buttonClasses, type ButtonSize, type ButtonVariant } from './Button'

/** A navigation link that looks like a Button ("Browse catalog", "Take attendance"). It stays a link for screen readers. */
export function LinkButton({ variant = 'primary', size = 'md', className, ...rest }: LinkProps & {
  variant?: ButtonVariant
  size?: ButtonSize
}) {
  return <Link {...rest} className={buttonClasses(variant, size, className)} />
}
