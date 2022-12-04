Describe 'Sample Tests 1' {
	Describe 'block 1' {
		It 'It 1 - success' {
			$true | Should -BeTrue -Because 'Test should pass'
		}

		It 'It 2 - fail' {
			$true | Should -BeFalse -Because 'Test should fail'
		}
	}
}
