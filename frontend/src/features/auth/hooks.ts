import { useMutation } from '@tanstack/react-query'
import { authApi } from './api'

export const useLogin = () => useMutation({ mutationFn: authApi.login })
export const useForgotPassword = () => useMutation({ mutationFn: authApi.forgotPassword })
export const useResetPassword = () => useMutation({ mutationFn: authApi.resetPassword })
